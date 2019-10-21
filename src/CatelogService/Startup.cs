using CatelogService.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using CatelogService.Model;
using System.Threading;

namespace CatelogService
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var sqlConnectionString = Configuration.GetConnectionString("CatelogService");
            services.AddDbContext<CatelogDbContext>(options => options.UseSqlServer(sqlConnectionString));
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
                scope.ServiceProvider.GetService<CatelogDbContext>().MigrateDB();
            }
            GetProductFromRabbitMQ();
        }
        private void GetProductFromRabbitMQ()
        {
            Task.Run(() =>
            {


                var queueName = "product";
                var rabbitMQ = Configuration.GetValue<string>("RabbitMQHost");
                var factory = new ConnectionFactory() { HostName = rabbitMQ };
                using (var connection = factory.CreateConnection())
                {

                    using (var channel = connection.CreateModel())
                    {
                        channel.QueueDeclare(queue: queueName,
                                             durable: false,
                                             exclusive: false,
                                             autoDelete: false,
                                             arguments: null);
                        byte[] body = null;
                        //using (var memorystream = new MemoryStream())
                        //{
                        //    var bf = new BinaryFormatter();
                        //    bf.Serialize(memorystream, product);
                        //    body = memorystream.ToArray();
                        //}
                        var consumer = new EventingBasicConsumer(channel);
                        consumer.Received += async (model, ea) =>
                       {
                           var body = ea.Body;
                           var message = Encoding.UTF8.GetString(body);
                           var sqlConnectionString = Configuration.GetConnectionString("CatelogService");
                           var dboptions = new DbContextOptionsBuilder<CatelogDbContext>().UseSqlServer(sqlConnectionString).Options;
                           var catelogDbContext = new CatelogDbContext(dboptions);
                           var product = JsonConvert.DeserializeObject<Product>(message);
                           if (product != null)
                           {
                               catelogDbContext.Add(new Product
                               {
                                   Name = product.Name,
                                   Price = product.Price
                               });
                               await catelogDbContext.SaveChangesAsync();
                           }
                       };
                        channel.BasicConsume(queue: queueName,
                                             autoAck: true,
                                             consumer: consumer);
                        while (true)
                        {
                            Thread.Sleep(1000);
                        }
                    }
                }
               
            });

        }
    }
}
