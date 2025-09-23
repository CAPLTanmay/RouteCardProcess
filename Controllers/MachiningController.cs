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
                var compositeKey = new CompositeKeyRequest
                {
                    WorkCenterNo = request.WorkCenterNo,
                    ProductionOrderNo = request.ProductionOrderNo, 
                    OperationNo = request.OperationNo
                };

                var existing = await _repo.GetByCompositeKeyAsync(compositeKey);


                //if (existing != null && !string.Equals(existing.MachiningStatus, "Completed", StringComparison.OrdinalIgnoreCase))
                    if (existing != null &&
                    !string.Equals(existing.MachiningStatus, "Completed", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(existing.MachiningStatus, "Partially Completed", StringComparison.OrdinalIgnoreCase))
                    {
                    bool isOperatorEnded = existing.OperatorEndTime != DateTime.MinValue;
                    bool isDifferentOperator = !string.IsNullOrEmpty(request.OperatorId) &&
                                               !string.Equals(existing.OperatorId, request.OperatorId, StringComparison.OrdinalIgnoreCase);
                    bool isMachingStopped=string.Equals(existing.MachiningStatus, "Machining Stopped", StringComparison.OrdinalIgnoreCase);

                    if ((isOperatorEnded || isDifferentOperator) && !isMachingStopped)
                    {
                        var operatorStartRequest = new MachiningOperatorStartRequest
                        {
                            MachiningId = existing.MachiningId,
                            OperatorId = request.OperatorId,
                            OperatorStartTime = DateTime.Now
                        };
                        // Insert into Trans_Machining_Operator only
                        await _repo.InsertMachiningOperatorStartAsync(operatorStartRequest);

                        return Ok(new
                        {
                            message = _userMessageService.GetMessage(1086), // "Operator started successfully"
                            MachiningId = existing.MachiningId,
                            machining = existing
                        });
                    }

                    // Existing machining and same operator continuing
                    return Ok(new
                    {
                        message = _userMessageService.GetMessage(1021), // "Machining already in progress"
                        MachiningId = existing.MachiningId,
                        machining = existing
                    });
                }

                // Machining doesn't exist or is completed → Create new
                var created = await _repo.CreateAsync(request);

                return Ok(new
                {
                    message = _userMessageService.GetMessage(1022), // "Machining created successfully"
                    MachiningId = created.MachiningId,
                    machining = created
                });
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("MachiningController", "check-or-create-machining", ex.ToString());

                if (ex.Message == _userMessageService.GetMessage(1061))
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

                var result = await _repo.StartMachiningAsync(request);

                return result.Message == _userMessageService.GetMessage(1082) // "Machining started"
                    ? Ok(result)         
                    : BadRequest(result); 
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("MachiningController", "start-machining", ex.ToString());
                return StatusCode(500, new
                {
                    message = _userMessageService.GetMessage(5005),
                    error = ex.Message
                });
            }
        }

        [HttpPost("toggle-pause")]
        public async Task<IActionResult> TogglePause([FromBody] MachiningPauseRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.MachiningId))
                    return BadRequest(new { message = _userMessageService.GetMessage(1024) });

                await _repo.TogglePauseAsync(request); // PauseCode can be null or empty
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

                await _repo.EndMachiningAsync(request);
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

                if (string.IsNullOrWhiteSpace(request.MachiningId))
                    return BadRequest(new { success = false, message = _userMessageService.GetMessage(1029) });

                // Call the updated method and capture result
                var result = await _repo.ProcessQuantitiesAsync(request);

                if (!result.Success)
                {
                    return Ok(new
                    {
                        success = false,
                        message = result.Message
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = _userMessageService.GetMessage(1030),
                    data = new { request.MachiningId, request.QuantityList }
                });
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("MachiningController", "add-quantity", ex.ToString());
                return StatusCode(500, new
                {
                    success = false,
                    message = _userMessageService.GetMessage(1003),
                    error = ex.Message
                });
            }
        }


        [HttpPost("add-delays")]
        public async Task<IActionResult> AddDelays([FromBody] MachiningDelayRequest request)
        {
            try
            {
                var result = await _repo.AddDelaysAsync(request);

                return result
                    ? Ok(new { message = _userMessageService.GetMessage(1034) })
                    : BadRequest(new { message = _userMessageService.GetMessage(1035) });
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("MachiningController", "add-delays", ex.ToString());
                return StatusCode(500, new { message = _userMessageService.GetMessage(5005), error = ex.Message });
            }
        }


        [HttpGet("{machiningId}")]
        public async Task<IActionResult> GetById(string machiningId)
        {
            try
            {
                var machining = await _repo.GetByCompositeKeyAsync(new CompositeKeyRequest
                {
                    WorkCenterNo = machiningId,
                    ProductionOrderNo = string.Empty,
                    OperationNo = string.Empty
                });

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
