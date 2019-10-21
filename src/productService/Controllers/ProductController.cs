using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using productService.Data;
using productService.Model;
using RabbitMQ.Client;

namespace productService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly ProductDbContext dbContext;
        private readonly IConfiguration configuration;

        public ProductController(ProductDbContext dbContext, IConfiguration configuration)
        {
            this.dbContext = dbContext;
            this.configuration = configuration;
        }

        [HttpGet]
        public IActionResult Get()
        {
            return Ok(dbContext.Products.ToList());
        }

        [HttpPost]
        public async Task<IActionResult> Post(Product product)
        {
            if (ModelState.IsValid)
            {
                dbContext.Products.Add(product);
                await dbContext.SaveChangesAsync();
                SendUpdateToCatelog(product);
                return Ok(product);
            }
            return BadRequest();
        }
        private void SendUpdateToCatelog(Product product)
        {
            var queueName = "product";
            var  rabbitMQ = configuration.GetValue<string>("RabbitMQHost");
            var factory = new ConnectionFactory() { HostName = rabbitMQ };
            using (var connection = factory.CreateConnection())

            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: "product",
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
               var msg = JsonConvert.SerializeObject(product);
                body = Encoding.UTF8.GetBytes(msg);
                channel.BasicPublish(exchange: "",
                                     routingKey: queueName,
                                     basicProperties: null,
                                     body: body);
                Console.WriteLine(" [x] Sent {0}", product.Name);
            }
        }
    }
}