using System.Net;
using System.Text.Json;
using ExecutingDevice;
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
				url = "https://192.168.150.4:44356"
			},
			new Device{
				name = "device2",
				url = "https://192.168.150.5:44357"
			},
			new Device{
				name = "device3",
				url = "https://192.168.150.6:44358"
			}
		});
	}

	private List<Device> Devices = new List<Device>();

	
	[HttpGet(Name = "GetDeviceStatus")]
	public async Task<IActionResult> GetStatus(string deviceName)
	{
        HttpClientHandler clientHandler = new HttpClientHandler();
        clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
        
		HttpClient client = new HttpClient(clientHandler);


		var device = Devices.FirstOrDefault(i => i.name == deviceName);

		if (device == null)
		{
			//_logger.LogInformation($"{DateTime.UtcNow} | It was not possible to get the {deviceName} status");

			return NotFound("Device not found");
		}
        
		var response = await client.GetAsync($"{device.url}/DeviceInfo/GetStatus");
		if (response.StatusCode == HttpStatusCode.OK)
		{
			var content = await response.Content.ReadAsStringAsync();
			var deviceStatus = JsonSerializer.Deserialize<DeviceStatus>(content);
        
			//_logger.LogInformation($"{DateTime.UtcNow} | {deviceStatus.deviceName} status: {deviceStatus.status}");
			return new JsonResult(new {deviceStatus.deviceName, deviceStatus.status});
		}
		else
		{
			//_logger.LogInformation($"{DateTime.UtcNow} | It was not possible to get the {deviceName} status");
			return NotFound(response.StatusCode);
		}
	}
	
	[HttpGet(Name = "GetAllDeviceStatus")]
	public async Task<IActionResult> GetAllDeviceStatus()
	{
		HttpClientHandler clientHandler = new HttpClientHandler();
		clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
        
		HttpClient client = new HttpClient(clientHandler);

		List<DeviceStatus> resultAllDevices = new List<DeviceStatus>();

		foreach (var device in Devices)
		{
			var response = await client.GetAsync($"{device.url}/DeviceInfo/GetStatus");
			if (response.StatusCode == HttpStatusCode.OK)
			{
				var content = await response.Content.ReadAsStringAsync();
				var deviceStatus = JsonSerializer.Deserialize<DeviceStatus>(content);
				resultAllDevices.Add(deviceStatus);
			}
		}
		
        if(resultAllDevices.Count == 0)
	        return NotFound("Devices not found");

        return new JsonResult(new { resultAllDevices });
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
		
		HttpClientHandler clientHandler = new HttpClientHandler();
		clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
		
		HttpClient client = new HttpClient(clientHandler);
		var response = client.PostAsync("https://192.168.150.2:44323/Heads/PostDeviceData", content);
            
		_logger.LogInformation($"{DateTime.UtcNow} | POST data to main device with {deviceData.name}. {response.Result.StatusCode}");

		return Ok();
	}
	
	[HttpPost(Name = "PostChangeStatus")]
	public async Task<IActionResult> PostChangeStatus(string deviceName, int value)
	{
		HttpClientHandler clientHandler = new HttpClientHandler();
		clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
        
		HttpClient client = new HttpClient(clientHandler);
		
		string newStatus;
		string oldStatus;
		
		var device = Devices.FirstOrDefault(i => i.name == deviceName);

		if (device == null)
		{
			return NotFound("Device not found");
		}
		
		var response = await client.GetAsync($"{device.url}/DeviceInfo/GetStatus");
		if (response.StatusCode == HttpStatusCode.OK)
		{
			var content = await response.Content.ReadAsStringAsync();
			var deviceStatus = JsonSerializer.Deserialize<DeviceStatus>(content);
			oldStatus = deviceStatus.status;
		}
		else
		{
			//_logger.LogInformation($"{DateTime.UtcNow} | It was not possible to get the {deviceName} status");
			return NotFound(response.StatusCode);
		}
        
		switch (value)
		{
			case 0:
				newStatus = Status.STOP;
				break;
			case 1:
				newStatus = Status.RUN;
				break;
			default:
				return NotFound("Not is status");
				break;
		}
		
		if(newStatus.Equals(oldStatus))
			return NotFound("Old status equals new status");
		
		var responsePost = await client.PostAsync($"{device.url}/DeviceInfo/PostChangeStatus?value={value}", null);
		return Ok();
	}

	[HttpPost(Name = "PostRunAllDevices")]
	public async Task<IActionResult> PostRunAllDevices()
	{
		foreach (var device in Devices)
		{
			HttpClientHandler clientHandler = new HttpClientHandler();
			clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
			HttpClient client = new HttpClient(clientHandler);
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
			HttpClientHandler clientHandler = new HttpClientHandler();
			clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
			HttpClient client = new HttpClient(clientHandler);
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
		public byte[] data { get; set; }
	}
    
	private class DeviceStatus
	{
		public string deviceName { get; set; }
		public string status { get; set; }
	}
}