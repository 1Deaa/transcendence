namespace HrmSystem.Application.Common.Exceptions;

/*
    //! - Wrapped the [[SaveChangesAsync()]] Body of the [[ApplicationDbContext]] in try catch
    //!        => To support [[  Optimistic Concurrency  ]]! =>
    //!     - To Solve any [[Race Conditions]] that we could have in [[Application Project]] (As Handlers which orchestrate logic)
    //* - I Created an (Custom) [Exception] and not returning [DbUpdateConcurrencyException] immediately! ==>> To not leak [EF Core] Details into my [Application Project]; so I abstracted EF Core behind this
    //? - My [ApplicationDbContext] which is my [UOF] will be handling the (Db Concurrency Exception) and throwing an exception: (DbConcurrencyProblemException) that is known to my (Application Project)
    //> Example of how can I handle it; just by wrapping in (try catch block) ==>> then return (Result Pattern Error):
        ```C# Code Example
            //! => This is inside of a Handler in the (Application Project) that is calling [[SaveChangesAsync()]] of the [[ApplicationDbContext]] which is my [UOF]
            try
                {
                    var booking = Booking.Reserve(
                        apartment,
                        user.Id,
                        duration,
                        _dateTimeProvider.UtcNow,
                        _pricingService);

                    _bookingRepository.Add(booking);

                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    return booking.Id;
                }
                catch (DbConcurrencyProblemException)
                {
                    return Result.Failure<Guid>(BookingErrors.Overlap);
                }
        ```
    //> - Consider that I can modify the above code and Add the Error Message of the Exception to be more specific and helpful for the Client, and I can also log the exception details for further inspecting and debugging
*/
public sealed class DbConcurrencyProblemException : Exception
{
    public DbConcurrencyProblemException(string message, Exception innerException)
        : base(message, innerException) { }
}
