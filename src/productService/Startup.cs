using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using productService.Data;

namespace productService
{
    public class Startup
    {
    //    private readonly ILoggerFactory loggerFactor;
    //    private readonly ILogger logger;
        //public Startup(ILoggerFactory loggerFactor)
        //{
        //    this.loggerFactor = loggerFactor;
        //    this.logger = loggerFactor.CreateLogger<Startup>();
        //}
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //ILogger<Startup> logger = 
            //logger.LogInformation("Starting the app");
            var sqlConnectionString = Configuration.GetConnectionString("ProductService");
            
            services.AddDbContext<ProductDbContext>(options => options.UseSqlServer(sqlConnectionString));
            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
            using (var scope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                scope.ServiceProvider.GetService<ProductDbContext>().MigrateDB();
            }
        }
    }
}
