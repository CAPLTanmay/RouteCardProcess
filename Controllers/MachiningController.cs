using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RouteCardProcess.Interfaces;
using RouteCardProcess.Model.DTOs.Machining;
using RouteCardProcess.Repositories;

namespace RouteCardProcess.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MachiningController : ControllerBase
    {
        private readonly IMachiningRepository _repo;
        private readonly ISystemLoggerRepository _systemLogger;
        private readonly IUserMessageService _userMessageService;

        public MachiningController(IMachiningRepository repo, ISystemLoggerRepository systemLogger, IUserMessageService userMessageService)
        {
            _repo = repo;
            _systemLogger = systemLogger;
            _userMessageService = userMessageService;
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
                    return BadRequest(new { message = _userMessageService.GetMessage(1020)});
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
                        message = _userMessageService.GetMessage(1021),
                        machiningID = existing.MachiningId,
                        machining = existing,
                    });
                }

                var created = await _repo.CreateAsync(request);
                return CreatedAtAction(nameof(GetById), new { machiningId = created.MachiningId }, new
                {
                    message = _userMessageService.GetMessage(1022),
                    machiningID = created.MachiningId,
                    machining = created
                });
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("MachiningController", "CheckOrCreateMachining", ex.ToString());
                if (ex.Message == "Invalid Operator ID")
                    return BadRequest(new { message = ex.Message });

                return StatusCode(500, new { message = _userMessageService.GetMessage(5005), error = ex.Message });
            }
        }

        [HttpPost("start-machining")]
        public async Task<IActionResult> StartMachining([FromBody] MachiningIdentifierRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.MachiningId))
                    return BadRequest(new { message = _userMessageService.GetMessage(1024) });

                await _repo.StartMachiningAsync(request.MachiningId);
                return Ok(new { message = _userMessageService.GetMessage(1025) });
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("MachiningController", "start-machining", ex.ToString());
                return StatusCode(500, new { message = _userMessageService.GetMessage(5005), error = ex.Message });
            }
        }

        [HttpPost("toggle-pause")]
        public async Task<IActionResult> TogglePause([FromBody] MachiningPauseRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.MachiningId))
                    return BadRequest(new { message = _userMessageService.GetMessage(1024) });

                await _repo.TogglePauseAsync(request.MachiningId, request.PauseCode); // PauseCode can be null or empty
                return Ok(new { message = _userMessageService.GetMessage(1026) });
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("MachiningController", "toggle-pause", ex.ToString());
                return StatusCode(500, new { message = _userMessageService.GetMessage(5005), error = ex.Message });
            }
        }


        [HttpPost("end-machining")]
        public async Task<IActionResult> EndOperatorTime([FromBody] MachiningIdentifierRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.MachiningId))
                    return BadRequest(new { message = _userMessageService.GetMessage(1024) });

                await _repo.EndMachiningAsync(request.MachiningId);
                return Ok(new { message =  _userMessageService.GetMessage(1027) });
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("MachiningController", "end-machining", ex.ToString());
                return StatusCode(500, new { message = _userMessageService.GetMessage(5005), error = ex.Message });
            }
        }

        [HttpPost("add-quantity")]
        public async Task<IActionResult> AddQuantity([FromBody] AddQuantity request)
        {
            try
            {
                if (request?.QuantityList == null || !request.QuantityList.Any())
                    return BadRequest(new { success = false, message = _userMessageService.GetMessage(1028) });

                if (string.IsNullOrWhiteSpace(request.MachiningId) || string.IsNullOrWhiteSpace(request.TotalQty))
                    return BadRequest(new { success = false, message = _userMessageService.GetMessage(1029) });

                var totalProcessed = request.QuantityList.Sum(q => int.Parse(q.ProcessedQty));
                // Step 1: Insert Quantities
                await _repo.AddQuantitiesAsync(request.MachiningId, int.Parse(request.TotalQty), totalProcessed, "Processed");

                // Step 2: Update Machining Status
                await _repo.UpdateMachiningStatusAsync(request.MachiningId);

                return Ok(new
                {
                    success = true,
                    message =  _userMessageService.GetMessage(1030),
                    data = new { request.MachiningId, request.TotalQty, request.QuantityList }
                });
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("MachiningController", "add-quantity", ex.ToString());
                return StatusCode(500, new { success = false, message = _userMessageService.GetMessage(1003), error = ex.Message });
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
                        message = _userMessageService.GetMessage(1034),
                        data = new { request.MachiningId, request.TotalDelayedTime, request.Delays }
                    });

                if (string.IsNullOrWhiteSpace(request.MachiningId))
                    return BadRequest(new { success = false, message = _userMessageService.GetMessage(1024) });

                var totalProcessed = request.Delays.Sum(d => int.Parse(d.ProcessedQty));
                var delayCode = request.Delays.First().DelayReasonCode;
                var delayTime = request.TotalDelayedTime ?? TimeSpan.Zero;

                await _repo.AddDelaysAsync(request.MachiningId, totalProcessed, delayTime, delayCode, delayTime);

                return Ok(new
                {
                    success = true,
                    message = _userMessageService.GetMessage(1034),
                    data = new { request.MachiningId, request.TotalDelayedTime, request.Delays }
                });
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("MachiningController", "add-delays", ex.ToString());
                return StatusCode(500, new { success = false, message = _userMessageService.GetMessage(1003), error = ex.Message });
            }
        }

        [HttpGet("{machiningId}")]
        public async Task<IActionResult> GetById(string machiningId)
        {
            try
            {
                var machining = await _repo.GetByCompositeKeyAsync(machiningId, string.Empty, string.Empty);
                return machining == null
                    ? NotFound(new { message = _userMessageService.GetMessage(1033) })
                    : Ok(machining);
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("MachiningController", "GetById", ex.ToString());
                return StatusCode(500, new { message = _userMessageService.GetMessage(5001) });
            }
        }
    }
}
