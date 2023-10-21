﻿using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace GateWay.Controllers;

[ApiController]
[Route("[controller]/[action]")]
public class GatewayController : ControllerBase
{
	private readonly ILogger<GatewayController> _logger;
	private readonly IConfiguration _config;

	public GatewayController(ILogger<GatewayController> logger, IConfiguration config)
	{
		_logger = logger;
		_config = config;
		
		Devices.AddRange(new List<Device>(){
			new Device{
				name = "device1",
				url = "https://localhost:7179"
			}
		});
	}

	private List<Device> Devices = new List<Device>();

	
	[HttpGet(Name = "GetDeviceStatus")]
	public async Task<IActionResult> GetStatus(string deviceName)
	{
		HttpClient client = new HttpClient();


		var device = Devices.FirstOrDefault(i => i.name == deviceName);

		if (device == null)
		{
			_logger.LogInformation($"{DateTime.UtcNow} | It was not possible to get the {deviceName} status");

			return NotFound();
		}
			
		
		var response = await client.GetAsync($"{device.url}/DeviceInfo/GetStatus");
		if (response.StatusCode == HttpStatusCode.OK)
		{
			var content = await response.Content.ReadAsStringAsync();
			var deviceStatus = JsonSerializer.Deserialize<DeviceStatus>(content);
        
			_logger.LogInformation($"{DateTime.UtcNow} | {deviceStatus.deviceName} status: {deviceStatus.status}");
			return new JsonResult(new {deviceStatus.deviceName, deviceStatus.status});
		}
		else
		{
			_logger.LogInformation($"{DateTime.UtcNow} | It was not possible to get the {deviceName} status");
			return NotFound();
		}
        
	}
	
	[HttpPost(Name ="PostDeviceData")]
	public async Task<IActionResult> PostDeviceData()
	{
		string body = string.Empty; 
		using (var reader = new StreamReader(Request.Body))
		{
			body = await reader.ReadToEndAsync();
		}
		var deviceData = JsonSerializer.Deserialize<DeviceData>(body);
		//_logger.LogInformation($"{DateTime.UtcNow} | {deviceData.name}: {deviceData.data}");
		
		JsonContent content = JsonContent.Create(deviceData);
		HttpClient client = new HttpClient();
		var response = client.PostAsync("https://localhost:7149/Heads/PostDeviceData", content);
            
		_logger.LogInformation($"{DateTime.UtcNow} | POST data to main device with {deviceData.name}. {response.Result.StatusCode}");

		return Ok();
	}

	[HttpPost(Name = "PostRunAllDevices")]
	public async Task<IActionResult> PostRunAllDevices()
	{
		foreach (var device in Devices)
		{
			HttpClient client = new HttpClient();
			var response = client.PostAsync($"{device.url}/DeviceInfo/PostChangeStatus?value=1", null);
		}
		
		_logger.LogInformation($"{DateTime.UtcNow} | RUN all device");
		return Ok();
	}
	
	[HttpPost(Name = "PostStopAllDevices")]
	public async Task<IActionResult> PostStopAllDevices()
	{
		foreach (var device in Devices)
		{
			HttpClient client = new HttpClient();
			var response = client.PostAsync($"{device.url}/DeviceInfo/PostChangeStatus?value=0", null);
		}
		
		_logger.LogInformation($"{DateTime.UtcNow} | STOP all device");
		return Ok();
	}
	
	private class Device
	{
		public string name { get; set; }
		public string url { get; set; }
	}
	
	private class DeviceData
	{
		public string name { get; set; }
		public string data { get; set; }
	}
    
	private class DeviceStatus
	{
		public string deviceName { get; set; }
		public string status { get; set; }
	}
}