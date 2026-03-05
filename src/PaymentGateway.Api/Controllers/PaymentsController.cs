using System.Net;

using Microsoft.AspNetCore.Mvc;

using PaymentGateway.Application.Services;
using PaymentGateway.Contracts.Payments;
using PaymentGateway.Core.Errors;

namespace PaymentGateway.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentsController(IPaymentService paymentService) : ControllerBase
{
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(GetPaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<GetPaymentResponse> GetPayment(Guid id)
    {
        var result = paymentService.GetPayment(id);
        return result.Match<ActionResult<GetPaymentResponse>>(
            Left: error => error switch
            {
                PaymentError.NotFound => NotFound(),
                _ => StatusCode(500)
            },
            Right: response => Ok(response)
        );
    }

    [HttpPost]
    [ProducesResponseType(typeof(GetPaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RejectedPaymentResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<GetPaymentResponse>> PostPayment(CreatePaymentRequest request)
    {
        var result = await paymentService.ProcessPaymentAsync(request);
        return result.Match<ActionResult<GetPaymentResponse>>(
            Left: error => error switch
            {
                PaymentError.BankUnavailable => StatusCode((int)HttpStatusCode.ServiceUnavailable),
                _ => StatusCode((int)HttpStatusCode.InternalServerError)
            },
            Right: response => Ok(response)
        );
    }
}