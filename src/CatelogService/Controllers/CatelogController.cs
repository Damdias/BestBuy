using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CatelogService.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CatelogService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CatelogController : ControllerBase
    {
        private readonly CatelogDbContext catelogDbContext;

        public CatelogController(CatelogDbContext catelogDbContext)
        {
            this.catelogDbContext = catelogDbContext;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            return Ok(await catelogDbContext.Products.ToListAsync());
        }
    }
}