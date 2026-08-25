/**
 * WebGL model viewer.
 *
 * The only file in the project that knows three.js exists. It is imported dynamically the
 * first time a visitor opens a 3D view, so nobody pays for the library unless they use it.
 *
 * Design notes:
 *
 *   Render on demand.  A turntable that nobody is looking at still drains a laptop battery.
 *                      Frames are drawn only when something actually changed: the camera
 *                      moved, the model is auto-rotating, or the canvas resized. When the
 *                      viewer scrolls off screen or the tab is hidden, rendering stops dead.
 *
 *   No lighting rig.   RoomEnvironment generates a studio-quality image-based light probe
 *                      procedurally. It gives physically-based materials something credible
 *                      to reflect without shipping a multi-megabyte HDR file, which matters
 *                      enormously for how furniture and finishes read.
 *
 *   Total disposal.    Browsers allow around sixteen simultaneous WebGL contexts and
 *                      silently kill the oldest beyond that. Every geometry, material,
 *                      texture, render target and observer is released on teardown.
 *
 * @module scene
 */

import * as THREE from 'three';
import { OrbitControls } from 'three/addons/controls/OrbitControls.js';
import { GLTFLoader } from 'three/addons/loaders/GLTFLoader.js';
import { DRACOLoader } from 'three/addons/loaders/DRACOLoader.js';
import { RoomEnvironment } from 'three/addons/environments/RoomEnvironment.js';

/** Matches the Draco decoder shipped alongside the pinned three.js version in index.html. */
const DRACO_DECODER_PATH = 'https://www.gstatic.com/draco/versioned/decoders/1.5.7/';

const DEG2RAD = Math.PI / 180;

/**
 * Builds a viewer and starts loading the model.
 *
 * @param {HTMLElement} host Empty element the canvas is appended to and sized against.
 * @param {string} optionsJson Serialised SceneOptions; see Interop/SceneInterop.cs.
 * @param {{ invokeMethodAsync: (name: string, arg: unknown) => Promise<void> }} owner
 * @returns {Promise<object>} A handle with setAutoRotate, resetCamera and dispose.
 */
export async function createViewer(host, optionsJson, owner) {
    const options = JSON.parse(optionsJson);

    const renderer = new THREE.WebGLRenderer({
        antialias: true,
        alpha: !options.background,
        powerPreference: 'high-performance',
    });

    // Capping at 2 is the single most effective performance decision here. A 3x device
    // pixel ratio means 2.25x the fragment work for a difference almost nobody can see.
    renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
    renderer.setSize(host.clientWidth, host.clientHeight, false);

    // Filmic tone mapping keeps highlights on polished surfaces from clipping to flat white,
    // which is what makes untuned WebGL renders look like plastic.
    renderer.toneMapping = THREE.ACESFilmicToneMapping;
    renderer.toneMappingExposure = Math.pow(2, options.exposureStops ?? 0);
    renderer.outputColorSpace = THREE.SRGBColorSpace;

    renderer.domElement.classList.add('viewer__canvas');
    host.appendChild(renderer.domElement);

    const scene = new THREE.Scene();
    if (options.background) {
        scene.background = new THREE.Color(options.background);
    }

    const camera = new THREE.PerspectiveCamera(
        options.fieldOfView || 35,
        host.clientWidth / Math.max(host.clientHeight, 1),
        0.01,
        1000,
    );

    const environment = new THREE.PMREMGenerator(renderer);
    const environmentTexture = environment.fromScene(new RoomEnvironment(), 0.04).texture;
    scene.environment = environmentTexture;

    const controls = new OrbitControls(camera, renderer.domElement);
    controls.enableDamping = true;
    controls.dampingFactor = 0.06;
    controls.enablePan = false;
    // Stops the visitor from orbiting under the floor, where a model has no underside and
    // the contact shadow gives the illusion away.
    controls.maxPolarAngle = Math.PI * 0.52;
    controls.minPolarAngle = Math.PI * 0.08;
    controls.autoRotateSpeed = 0.6;
    controls.autoRotate = Boolean(options.autoRotate);

    let disposed = false;
    let needsRender = true;
    let onScreen = false;
    let frame = 0;
    let shadow = null;
    let model = null;
    /** @type {THREE.Vector3 | null} */
    let homeTarget = null;
    /** @type {THREE.Vector3 | null} */
    let homePosition = null;

    const requestRender = () => { needsRender = true; };
    controls.addEventListener('change', requestRender);

    // -----------------------------------------------------------------------
    // Frame loop
    // -----------------------------------------------------------------------

    function tick() {
        if (disposed) {
            return;
        }

        frame = requestAnimationFrame(tick);

        // Damping keeps easing the camera for a moment after the pointer is released,
        // so `update()` reports whether anything actually moved.
        const moved = controls.update();

        if (moved || needsRender) {
            needsRender = false;
            renderer.render(scene, camera);
        }
    }

    function start() {
        if (!frame && !disposed) {
            needsRender = true;
            frame = requestAnimationFrame(tick);
        }
    }

    function stop() {
        if (frame) {
            cancelAnimationFrame(frame);
            frame = 0;
        }
    }

    // -----------------------------------------------------------------------
    // Visibility: never render a viewer nobody can see
    // -----------------------------------------------------------------------

    const visibility = new IntersectionObserver(
        ([entry]) => {
            onScreen = entry.isIntersecting;
            if (onScreen && !document.hidden) {
                start();
            } else {
                stop();
            }
        },
        { threshold: 0 },
    );
    visibility.observe(host);

    const onVisibilityChange = () => {
        if (document.hidden) {
            stop();
        } else if (onScreen) {
            start();
        }
    };
    document.addEventListener('visibilitychange', onVisibilityChange);

    const resize = new ResizeObserver(() => {
        const width = host.clientWidth;
        const height = host.clientHeight;
        if (width === 0 || height === 0) {
            return;
        }

        camera.aspect = width / height;
        camera.updateProjectionMatrix();
        renderer.setSize(width, height, false);
        requestRender();
    });
    resize.observe(host);

    // -----------------------------------------------------------------------
    // Model loading
    // -----------------------------------------------------------------------

    const draco = new DRACOLoader().setDecoderPath(DRACO_DECODER_PATH);
    const loader = new GLTFLoader().setDRACOLoader(draco);

    try {
        const gltf = await loader.loadAsync(options.modelUrl, (event) => {
            if (event.lengthComputable) {
                owner.invokeMethodAsync('OnSceneProgress', event.loaded / event.total);
            }
        });

        model = gltf.scene;
        frameModel(model, camera, controls, options);
        scene.add(model);

        if (options.groundShadow) {
            shadow = createContactShadow(model);
            scene.add(shadow);
        }

        homePosition = camera.position.clone();
        homeTarget = controls.target.clone();

        // Render one frame synchronously before reporting readiness, so the poster is
        // replaced by a finished image rather than by an empty canvas.
        renderer.render(scene, camera);
        start();

        await owner.invokeMethodAsync('OnSceneReady', null);
    } catch (error) {
        disposeAll();
        await owner.invokeMethodAsync('OnSceneReady', String(error?.message ?? error));
        throw error;
    }

    // -----------------------------------------------------------------------
    // Teardown
    // -----------------------------------------------------------------------

    function disposeAll() {
        if (disposed) {
            return;
        }

        disposed = true;
        stop();

        visibility.disconnect();
        resize.disconnect();
        document.removeEventListener('visibilitychange', onVisibilityChange);
        controls.removeEventListener('change', requestRender);
        controls.dispose();

        // three.js does not garbage collect GPU resources; every one has to be released
        // by hand or the memory stays allocated until the tab closes.
        scene.traverse((node) => {
            if (!node.isMesh) {
                return;
            }

            node.geometry?.dispose();

            for (const material of Array.isArray(node.material) ? node.material : [node.material]) {
                if (!material) {
                    continue;
                }

                for (const value of Object.values(material)) {
                    if (value && value.isTexture) {
                        value.dispose();
                    }
                }

                material.dispose();
            }
        });

        environmentTexture.dispose();
        environment.dispose();
        draco.dispose();
        renderer.dispose();

        // Releases the GPU context immediately instead of waiting for the collector.
        renderer.forceContextLoss();
        renderer.domElement.remove();
    }

    return {
        setAutoRotate(enabled) {
            controls.autoRotate = Boolean(enabled);
            requestRender();
        },

        resetCamera() {
            if (homePosition && homeTarget) {
                camera.position.copy(homePosition);
                controls.target.copy(homeTarget);
                controls.update();
                requestRender();
            }
        },

        dispose: disposeAll,
    };
}

/**
 * Centres a model on the origin and pulls the camera back far enough to frame it.
 *
 * Authored camera distances are relative to the model's own size, so a chair and a whole
 * room can share the same scene settings and both arrive correctly framed.
 */
function frameModel(model, camera, controls, options) {
    const box = new THREE.Box3().setFromObject(model);
    const size = box.getSize(new THREE.Vector3());
    const centre = box.getCenter(new THREE.Vector3());

    // Sit the model on y = 0 and centre it horizontally, so the contact shadow and the
    // orbit target both land where a viewer expects.
    model.position.sub(new THREE.Vector3(centre.x, box.min.y, centre.z));

    const radius = Math.max(size.length() / 2, 0.001);
    const distance = radius * (options.cameraDistance || 2.2);

    const azimuth = (options.cameraAzimuth ?? 35) * DEG2RAD;
    const elevation = (options.cameraElevation ?? 12) * DEG2RAD;

    camera.position.set(
        distance * Math.cos(elevation) * Math.sin(azimuth),
        distance * Math.sin(elevation) + size.y * 0.5,
        distance * Math.cos(elevation) * Math.cos(azimuth),
    );

    controls.target.set(0, size.y * 0.45, 0);
    controls.minDistance = radius * 1.1;
    controls.maxDistance = radius * 8;
    controls.update();

    // Fit the clipping planes to the object. Leaving them at the defaults wastes depth
    // buffer precision and produces z-fighting on coplanar surfaces such as inset panels.
    camera.near = radius / 100;
    camera.far = radius * 100;
    camera.updateProjectionMatrix();
}

/**
 * A soft radial gradient on the ground plane.
 *
 * Far cheaper than shadow mapping and, for a single object on a neutral background, more
 * convincing: it reads as ambient occlusion rather than as a hard light source that is not
 * otherwise present in the scene.
 */
function createContactShadow(model) {
    const box = new THREE.Box3().setFromObject(model);
    const size = box.getSize(new THREE.Vector3());
    const radius = Math.max(size.x, size.z) * 0.75;

    const canvas = document.createElement('canvas');
    canvas.width = canvas.height = 256;

    const context = canvas.getContext('2d');
    const gradient = context.createRadialGradient(128, 128, 0, 128, 128, 128);
    gradient.addColorStop(0, 'rgba(0,0,0,0.42)');
    gradient.addColorStop(0.45, 'rgba(0,0,0,0.16)');
    gradient.addColorStop(1, 'rgba(0,0,0,0)');
    context.fillStyle = gradient;
    context.fillRect(0, 0, 256, 256);

    const texture = new THREE.CanvasTexture(canvas);
    texture.colorSpace = THREE.SRGBColorSpace;

    const mesh = new THREE.Mesh(
        new THREE.PlaneGeometry(radius * 2.4, radius * 2.4),
        new THREE.MeshBasicMaterial({
            map: texture,
            transparent: true,
            depthWrite: false,
        }),
    );

    mesh.rotation.x = -Math.PI / 2;
    // Just above the floor, to avoid z-fighting with anything else on the ground plane.
    mesh.position.y = 0.001;
    mesh.renderOrder = -1;

    return mesh;
}
