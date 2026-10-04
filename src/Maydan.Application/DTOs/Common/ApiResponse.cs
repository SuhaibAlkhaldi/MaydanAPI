namespace Maydan.Application.DTOs.Common;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string MessageAr { get; set; } = string.Empty;
    public string MessageEn { get; set; } = string.Empty;
    public T? Data { get; set; }

    // Workers module (2026-09-30): lets a Service communicate the intended HTTP status code
    // directly on the returned envelope instead of throwing — the Controller just does
    // `StatusCode(response.StatusCode, response)`. Defaults to 200 so every existing caller that
    // never sets it (ServiceConfigurationService, etc.) is unaffected.
    public int StatusCode { get; set; } = 200;

    public ApiResponse()
    {
    }

    public ApiResponse(bool success, string messageAr, string messageEn, T? data, int statusCode = 200)
    {
        Success = success;
        MessageAr = messageAr;
        MessageEn = messageEn;
        Data = data;
        StatusCode = statusCode;
    }
}
