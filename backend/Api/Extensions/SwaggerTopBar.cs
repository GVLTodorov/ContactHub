using System.Text.Json;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace Api.Extensions;

public static class SwaggerTopBar
{
    /// <summary>
    /// Adds a Client button to the Swagger UI top bar, right after the Swagger logo, that opens the
    /// Angular app. The URL comes from "ClientUrl" (default "/", the same origin). Add more
    /// [label, url] pairs to the list to get more buttons.
    /// </summary>
    public static void AddTopBarLinks(SwaggerUIOptions options, IConfiguration config)
    {
        var clientUrl = config["ClientUrl"] ?? "/";
        var links = JsonSerializer.Serialize(new[] { new[] { "Client", clientUrl } });

        options.HeadContent += $$"""
            <style>
              /* Scoped under .swagger-ui .topbar to outrank Swagger UI's own
                 `.swagger-ui nav { display: block }` and `.swagger-ui .topbar a` rules.
                 margin-right: auto keeps the buttons next to the logo and pushes the rest right.
                 The logo link normally stretches to 300px, so shrink it to the logo's width. */
              .swagger-ui .topbar .topbar-wrapper .link { flex: 0 0 auto; }
              .swagger-ui .topbar .topbar-links {
                display: flex; flex-direction: row; flex: 0 0 auto; gap: 8px;
                margin-left: 24px; margin-right: auto; align-items: center;
              }
              .swagger-ui .topbar .topbar-links a {
                flex: 0 0 auto; max-width: none;
                color: #fff; font-weight: 600; font-size: 14px; text-decoration: none;
                padding: 6px 14px; border: 2px solid #62a03f; border-radius: 4px;
              }
              .swagger-ui .topbar .topbar-links a:hover { background: #62a03f; }
            </style>
            <script>
              window.addEventListener('load', function () {
                var links = {{links}};
                var timer = setInterval(function () {
                  var bar = document.querySelector('.topbar-wrapper');
                  if (!bar || !bar.firstElementChild) return;
                  clearInterval(timer);
                  var nav = document.createElement('nav');
                  nav.className = 'topbar-links';
                  links.forEach(function (link) {
                    var a = document.createElement('a');
                    a.textContent = link[0];
                    a.href = link[1];
                    a.target = '_blank';
                    a.rel = 'noopener';
                    nav.appendChild(a);
                  });
                  bar.insertBefore(nav, bar.firstElementChild.nextSibling);
                }, 50);
              });
            </script>
            """;
    }
}
