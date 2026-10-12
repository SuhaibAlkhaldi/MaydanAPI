namespace Maydan.Application.DTOs.Common;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string MessageAr { get; set; } = string.Empty;
    public string MessageEn { get; set; } = string.Empty;
    public T? Data { get; set; }

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

    public static ApiResponse<T> SuccessResponse(T? data, string messageAr, string messageEn, int statusCode = 200)
    {
        return new ApiResponse<T>(true, messageAr, messageEn, data, statusCode);
    }

    public static ApiResponse<T> FailureResponse(string messageAr, string messageEn, int statusCode = 400)
    {
        return new ApiResponse<T>(false, messageAr, messageEn, default, statusCode);
    }
}