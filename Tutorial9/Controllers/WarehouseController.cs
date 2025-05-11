using Microsoft.AspNetCore.Mvc;
using Tutorial9.Model;
using Tutorial9.Services;

namespace Tutorial9.Controllers
{
    [Route("api/warehouse")]
    [ApiController]
    public class WarehouseController : ControllerBase
    {
        private readonly IDbService _dbService;

        public WarehouseController(IDbService dbService)
        {
            _dbService = dbService;
        }

        [HttpPost]
        public async Task<IActionResult> AddProductToWarehouse([FromBody] ProductWarehouseRequest request)
        {
            if (request == null || request.Amount <= 0)
            {
                return BadRequest("Invalid request data");
            }

            try
            {
                var newId = await _dbService.AddProductToWarehouseAsync(request);
                return Ok(new ProductWarehouseResponse { IdProductWarehouse = newId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("procedure")]
        public async Task<IActionResult> AddProductToWarehouseWithProcedure([FromBody] ProductWarehouseRequest request)
        {
            if (request == null || request.Amount <= 0)
            {
                return BadRequest("Invalid request data");
            }

            try
            {
                var newId = await _dbService.AddProductToWarehouseWithProcedureAsync(request);
                return Ok(new ProductWarehouseResponse { IdProductWarehouse = newId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}