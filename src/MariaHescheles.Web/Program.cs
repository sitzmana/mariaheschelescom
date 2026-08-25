using MariaHescheles.Web;
using MariaHescheles.Web.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");

// Lets any component contribute <title> and <meta> tags at runtime via <PageTitle> and
// <HeadContent>. Crawlers and link unfurlers that execute JavaScript see the result.
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddSiteServices(builder.HostEnvironment.BaseAddress);

await builder.Build().RunAsync();
