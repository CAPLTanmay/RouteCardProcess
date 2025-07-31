using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RouteCardProcess.Interfaces;
using RouteCardProcess.Model.DTOs.Employee;
using RouteCardProcess.Model.DTOs.RBACEmployee;
using RouteCardProcess.Repositories;
using System;
using System.Threading.Tasks;

namespace RouteCardProcess.Controllers
{
    [ApiController]
    [Route("api/[controller]")]

    public class RBACEmployeeController : ControllerBase
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly ISystemLoggerRepository _systemLogger;
        private readonly IUserMessageService _userMessageService;

        public RBACEmployeeController(IEmployeeRepository employeeRepository, ISystemLoggerRepository systemLogger, IUserMessageService userMessageService)
        {
            _employeeRepository = employeeRepository;
            _systemLogger = systemLogger;
            _userMessageService = userMessageService;
        }
        [HttpPost("addEmployee")]
        public async Task<IActionResult> AddEmployee([FromBody] EmployeeRequest request)
        {
            try
            {
                if (request.EmployeeCode == null)
                {
                    request.EmployeeCode = request.EmployeeId;

                }

                var result = await _employeeRepository.AddEmployeeAsync(request);

                if (result == "EXISTS") // assuming this is how you detect duplicates
                    return Ok(new { message = _userMessageService.GetMessage(1110) }); // Employee already exists

                if (result == "SUCCESS")
                    return Ok(new { message = _userMessageService.GetMessage(1109) }); // Employee added successfully

                return StatusCode(500, _userMessageService.GetMessage(1112)); // Failed to add employee
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("EmployeeController", "add", ex.ToString());
                return StatusCode(500, _userMessageService.GetMessage(5001)); // General error
            }
        }


        [HttpPost("updateEmployee")]
        public async Task<IActionResult> UpdateEmployee([FromBody] UpdateEmployeeRequest request)
        {
            try
            {
                var result = await _employeeRepository.UpdateEmployeeAsync(request);

                if (result == "SUCCESS")
                    return Ok(new { message = _userMessageService.GetMessage(1113) }); // Update successful

                return StatusCode(500, _userMessageService.GetMessage(1114)); // Update failed
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("EmployeeController", "update", ex.ToString());
                return StatusCode(500, _userMessageService.GetMessage(5001)); // General error
            }
        }


        [HttpGet("getAllEmployee")]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var employees = await _employeeRepository.GetAllEmployeesAsync();
                return Ok(employees);
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("EmployeeController", "get-all", ex.ToString());
                return StatusCode(500, _userMessageService.GetMessage(5001));
            }
        }

        [HttpPost("getById")]
        public async Task<IActionResult> GetById([FromBody] GetEmployeeRequest request)
        {
            try
            {
                var employee = await _employeeRepository.GetEmployeeByIdAsync(request);

                if (employee == null)
                {
                    return NotFound(new
                    {
                        IsSuccess = false,
                        Message = _userMessageService.GetMessage(1115),
                        Data = (object)null
                    });
                }

                return Ok(new
                {
                    IsSuccess = true,
                    Message = "Employee fetched successfully",
                    Data = employee
                });
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("EmployeeController", "get-by-id", ex.ToString());
                return StatusCode(500, new
                {
                    IsSuccess = false,
                    Message = _userMessageService.GetMessage(5001),
                    Data = (object)null
                });
            }
        }



        [HttpPost("deleteEmployee")]
        public async Task<IActionResult> SoftDelete([FromBody] DeleteEmployeeRequest request)
        {
            try
            {
                var result = await _employeeRepository.SoftDeleteEmployeeAsync(request.EmployeeId, request.UpdatedBy);
                return Ok(new { message = result });
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("EmployeeController", "delete", ex.ToString());
                return StatusCode(500, _userMessageService.GetMessage(5001));
            }
        }

        [HttpPost("resetTempPassword")]
        public async Task<IActionResult> ResetTempPassword([FromBody] ResetPasswordDto dto)
        {
            var result = await _employeeRepository.ResetTempPasswordAsync(dto);
            if (!result)
                return BadRequest(new { message = "Invalid operator ID or temp password." });

            return Ok(new { message = "Password reset successful." });
        }

    }
}
