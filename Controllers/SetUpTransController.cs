using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RouteCardProcess.Interfaces;
using RouteCardProcess.Model.DTOs.Setup;
using RouteCardProcess.Repositories;

namespace RouteCardProcess.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SetUpTransController : ControllerBase
    {
        private readonly ISetUpTransRepository _repo;
        private readonly ISystemLoggerRepository _systemLogger;
        private readonly IUserMessageService _userMessageService;
        public SetUpTransController(ISetUpTransRepository repo, ISystemLoggerRepository systemLogger, IUserMessageService userMessageService)
        {
            _repo = repo;
            _systemLogger = systemLogger;
            _userMessageService = userMessageService;
        }

        [HttpPost("check-status")]
        public async Task<IActionResult> CheckStatus([FromBody] SetupMasterDto request)
        {
            try
            {
                var result = await _repo.CheckSetupNotificationStatusAsync(
                    request.WorkCenterNo,
                    request.ProductionOrderNo,
                    request.OperationNo
                );

                return Ok(new
                {
                    flag = result.Flag,
                    setupStatus = result.SetupStatus,
                    machiningStatus = result.MachiningStatus,
                    setupId = result.SetUpID,
                    machiningId = result.MachiningID,
                    message = result.Message,
                    breakdown = result.Breakdown
                });
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("SetUpTransController", "check-status", ex.ToString());
                return StatusCode(500, new { message = _userMessageService.GetMessage(5005), error = ex.Message });
            }
        }

        [HttpPost("check-or-create")]
        public async Task<IActionResult> CheckOrCreateSetup([FromBody] SetupMasterDto request)
        {
            try
            {
                var compositeKey = new SetupCompositeKeyRequest
                {
                    WorkCenterNo = request.WorkCenterNo,
                    WorkOrderNo = request.ProductionOrderNo,
                    OperationNo = request.OperationNo
                };

                var existing = await _repo.GetByCompositeKeyAsync(compositeKey);

                if (existing != null && !string.Equals(existing.SetupStatus, "Completed", StringComparison.OrdinalIgnoreCase))
                {
                    bool isOperatorEnded = existing.OperatorEndTime != DateTime.MinValue;
                    bool isDifferentOperator = !string.IsNullOrEmpty(request.OperatorId) &&
                                               !string.Equals(existing.OperatorId, request.OperatorId, StringComparison.OrdinalIgnoreCase);
                    bool isSetupStopped =string.Equals(existing.SetupStatus, "Setup Stopped", StringComparison.OrdinalIgnoreCase);

                    if ((isOperatorEnded || isDifferentOperator) && !isSetupStopped)
                    {
                        // Insert into Trans_Setup_Operator only
                        await _repo.InsertSetupOperatorStartAsync(existing.SetUpID, request.OperatorId, DateTime.Now);

                        return Ok(new
                        {
                            message = _userMessageService.GetMessage(1086), // e.g. "Operator started successfully"
                            SetUpID = existing.SetUpID,
                            setup = existing
                        });
                    }

                    // Existing setup and same operator continuing
                    return Ok(new
                    {
                        message = _userMessageService.GetMessage(1086), // e.g. "Setup already in progress"
                        SetUpID = existing.SetUpID,
                        setup = existing
                    });
                }

                // Setup doesn't exist or it's completed → Create new
                var created = await _repo.CreateSetupAsync(request);

                return Ok(new
                {
                    message = _userMessageService.GetMessage(1085), // e.g. "Setup created successfully"
                    SetUpId = created.SetUpID,
                    setup = created
                });
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("SetUpTransController", "check-or-create", ex.ToString());

                if (ex.Message == _userMessageService.GetMessage(1061))
                    return BadRequest(new { message = ex.Message });

                return StatusCode(500, new { message = _userMessageService.GetMessage(5005), error = ex.Message });
            }
        }

        [HttpPost("start-setup")]
        public async Task<IActionResult> StartSetup([FromBody] SetupIdentifierRequest request)
        {
            try
            {
                var result = await _repo.StartSetupAsync(request);

                return result.Message == _userMessageService.GetMessage(1056)
                    ? Ok(result)         // includes message and OperatorStartTime
                    : BadRequest(result); // includes message and OperatorStartTime
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("SetUpTransController", "start-setup", ex.ToString());
                return StatusCode(500, new
                {
                    message = _userMessageService.GetMessage(5005),
                    error = ex.Message
                });
            }
        }

        [HttpPost("toggle-pause")]
        public async Task<IActionResult> TogglePause([FromBody] SetupPauseRequest request)
        {
            try
            {
                var result = await _repo.TogglePauseAsync(request);

                return (result == _userMessageService.GetMessage(1075) || result == _userMessageService.GetMessage(1059))
                    ? Ok(new { message = result })
                    : BadRequest(new { message = result });
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("SetUpTransController", "toggle-pause", ex.ToString());
                return StatusCode(500, new { message = _userMessageService.GetMessage(5005), error = ex.Message });
            }
        }

        [HttpPost("end-setup")]
        public async Task<IActionResult> EndOperatorTime([FromBody] SetupIdentifierRequest request)
        {
            try
            {
                var result = await _repo.EndSetupTimeAsync(request);

                if (result.Success)
                    return Ok(result);
                else
                    return NotFound(result);
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("SetUpTransController", "end-setup", ex.ToString());
                return StatusCode(500, new EndSetupResultDto
                {
                    Success = false,
                    Message = _userMessageService.GetMessage(5005),
                    TimeDiff = null
                });
            }
        }

        [HttpPost("add-delays")]
        public async Task<IActionResult> AddDelays([FromBody] SetupDelayRequest request)
        {
            try
            {
                var result = await _repo.InsertDelaysAsync(request);

                return result
                    ? Ok(new { message = _userMessageService.GetMessage(1034) })
                    : BadRequest(new { message = _userMessageService.GetMessage(1035) });
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("SetUpTransController", "add-delays", ex.ToString());
                return StatusCode(500, new { message = _userMessageService.GetMessage(5005), error = ex.Message });
            }
        }
    }
}
