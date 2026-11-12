using System.Text.Json;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace Api.Swagger;

public static class SwaggerTopBar
{
    public static void AddTopBarLinks(SwaggerUIOptions options, IConfiguration config)
    {
        var clientUrl = config["ClientUrl"] ?? "/";
        var links = JsonSerializer.Serialize(new[] { new[] { "Client", clientUrl } });

        options.HeadContent += """
            <style>
            .swagger-ui .topbar .topbar-links { display: flex; flex-direction: row; gap: 8px; align-items: center; margin-right: 16px; }
            .swagger-ui .topbar .topbar-links a { color: #fff; border: 1px solid #fff; border-radius: 4px; padding: 6px 14px; text-decoration: none; font-family: sans-serif; font-size: 14px; }
            .swagger-ui .topbar .topbar-links a:hover { background: rgba(255,255,255,0.15); }
            </style>
            """;
        options.HeadContent += $$"""
            <script>
            (function () {
              var links = {{links}};
              var timer = setInterval(function () {
                var anchor = document.querySelector('.topbar-wrapper .download-url-wrapper');
                if (!anchor) return;
                clearInterval(timer);
                var nav = document.createElement('nav');
                nav.className = 'topbar-links';
                links.forEach(function (l) {
                  var a = document.createElement('a');
                  a.textContent = l[0];
                  a.href = l[1];
                  a.target = '_blank';
                  nav.appendChild(a);
                });
                anchor.parentNode.insertBefore(nav, anchor);
              }, 50);
            })();
            </script>
            """;
    }
}
