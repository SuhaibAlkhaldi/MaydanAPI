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

    public static ApiResponse<T> SuccessResponse(T data, string messageEn = "Success", string messageAr = "تمت العملية بنجاح", int statusCode = 200)
    {
        return new ApiResponse<T>
        {
            Success = true,
            MessageEn = messageEn,
            MessageAr = messageAr,
            Data = data,
            StatusCode = statusCode
        };
    }

    public static ApiResponse<T> FailureResponse(string messageEn, string messageAr)
    {
        return new ApiResponse<T>
        {
            Success = false,
            MessageEn = messageEn,
            MessageAr = messageAr,
            Data = default,
            StatusCode = 400
        };
    }
}
