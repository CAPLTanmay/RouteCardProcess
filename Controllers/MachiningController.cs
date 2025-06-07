using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RouteCardProcess.Interfaces;
using RouteCardProcess.Model.DTOs.Machining;
using RouteCardProcess.Model.Entities;
using Microsoft.Extensions.Logging;

namespace RouteCardProcess.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MachiningController : ControllerBase
    {
        private readonly IMachiningRepository _repo;
        private readonly ILogger<MachiningController> _logger;

        public MachiningController(IMachiningRepository repo, ILogger<MachiningController> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        [HttpPost("check-or-create")]
        public async Task<IActionResult> CheckOrCreateMachining([FromBody] MachiningDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.WorkCenterNo) ||
                    string.IsNullOrWhiteSpace(request.WorkOrderNo) ||
                    string.IsNullOrWhiteSpace(request.OperationNo))
                {
                    return BadRequest(new { message = "WorkCenterNo, WorkOrderNo, and OperationNo are required." });
                }

                var existing = await _repo.GetByCompositeKeyAsync(request.WorkCenterNo, request.WorkOrderNo, request.OperationNo);

                if (existing != null)
                {
                    var startTime = existing.MachiningStartTime;
                    var endTime = existing.MachiningEndTime ?? DateTime.Now;

                    TimeSpan? adjustedTotalTime = null;

                    if (startTime.HasValue)
                        adjustedTotalTime = endTime - startTime;

                    return Ok(new
                    {
                        message = "Machining already exists",
                        machiningID = existing.MachiningId,
                        machining = existing,
                    });
                }

                var created = await _repo.CreateAsync(request);
                return CreatedAtAction(nameof(GetById), new { machiningId = created.MachiningId }, new
                {
                    message = "New machining created",
                    machiningID = created.MachiningId,
                    machining = created
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating machining record.");
                if (ex.Message == "Invalid Operator ID")
                    return BadRequest(new { message = ex.Message });

                return StatusCode(500, new { message = "An unexpected error occurred.", error = ex.Message });
            }
        }

        [HttpPost("start-machining")]
        public async Task<IActionResult> StartMachining([FromBody] MachiningIdentifierRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.MachiningId))
                    return BadRequest(new { message = "MachiningId is required." });

                await _repo.StartMachiningAsync(request.MachiningId);
                return Ok(new { message = "Machining started successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while starting machining process.");
                return StatusCode(500, new { message = "An unexpected error occurred.", error = ex.Message });
            }
        }

        [HttpPost("toggle-pause")]
        public async Task<IActionResult> TogglePause([FromBody] MachiningPauseRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.MachiningId))
                    return BadRequest(new { message = "MachiningId is required." });

                await _repo.TogglePauseAsync(request.MachiningId, request.PauseCode); // PauseCode can be null or empty
                return Ok(new { message = "Machining pause toggled successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while toggling machining pause.");
                return StatusCode(500, new { message = "An unexpected error occurred.", error = ex.Message });
            }
        }


        [HttpPost("end-machining")]
        public async Task<IActionResult> EndOperatorTime([FromBody] MachiningIdentifierRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.MachiningId))
                    return BadRequest(new { message = "MachiningId is required." });

                await _repo.EndMachiningAsync(request.MachiningId);
                return Ok(new { message = "Operator end time updated successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while ending machining process.");
                return StatusCode(500, new { message = "An unexpected error occurred.", error = ex.Message });
            }
        }

        [HttpPost("add-quantity")]
        public async Task<IActionResult> AddQuantity([FromBody] AddQuantity request)
        {
            try
            {
                if (request?.QuantityList == null || !request.QuantityList.Any())
                    return BadRequest(new { success = false, message = "Invalid input: QuantityList cannot be empty." });

                if (string.IsNullOrWhiteSpace(request.MachiningId) || string.IsNullOrWhiteSpace(request.TotalQty))
                    return BadRequest(new { success = false, message = "MachiningId and TotalQty are required." });

                var totalProcessed = request.QuantityList.Sum(q => int.Parse(q.ProcessedQty));
                // Step 1: Insert Quantities
                await _repo.AddQuantitiesAsync(request.MachiningId, int.Parse(request.TotalQty), totalProcessed, "Processed");

                // Step 2: Update Machining Status
                await _repo.UpdateMachiningStatusAsync(request.MachiningId);

                return Ok(new
                {
                    success = true,
                    message = "Quantity inserted and machining status updated successfully",
                    data = new { request.MachiningId, request.TotalQty, request.QuantityList }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding quantities.");
                return StatusCode(500, new { success = false, message = "Insertion failed", error = ex.Message });
            }
        }

        [HttpPost("add-delays")]
        public async Task<IActionResult> AddDelays([FromBody] MachiningDelayRequest request)
        {
            try
            {
                if (request?.Delays == null || !request.Delays.Any())
                    return Ok(new
                    {
                        success = true,
                        message = "Delays inserted successfully",
                        data = new { request.MachiningId, request.TotalDelayedTime, request.Delays }
                    });

                if (string.IsNullOrWhiteSpace(request.MachiningId))
                    return BadRequest(new { success = false, message = "MachiningId is required." });

                var totalProcessed = request.Delays.Sum(d => int.Parse(d.ProcessedQty));
                var delayCode = request.Delays.First().DelayReasonCode;
                var delayTime = request.TotalDelayedTime ?? TimeSpan.Zero;

                await _repo.AddDelaysAsync(request.MachiningId, totalProcessed, delayTime, delayCode, delayTime);

                return Ok(new
                {
                    success = true,
                    message = "Delays inserted successfully",
                    data = new { request.MachiningId, request.TotalDelayedTime, request.Delays }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding delays.");
                return StatusCode(500, new { success = false, message = "Insertion failed", error = ex.Message });
            }
        }

        [HttpGet("{machiningId}")]
        public async Task<IActionResult> GetById(string machiningId)
        {
            try
            {
                var machining = await _repo.GetByCompositeKeyAsync(machiningId, string.Empty, string.Empty);
                return machining == null
                    ? NotFound(new { message = "Machining record not found." })
                    : Ok(machining);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching machining record.");
                return StatusCode(500, new { message = "Internal server error." });
            }
        }
    }
}
