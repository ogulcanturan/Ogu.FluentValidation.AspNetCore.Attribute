# Ogu.FluentValidation.AspNetCore.Attributes

## Introduction

Ogu.FluentValidation.AspNetCore.Attributes is a library that extends [FluentValidation](https://github.com/FluentValidation/FluentValidation) with additional attributes to simplify validation in ASP.NET Core applications. It allows you to specify validation rules directly on the model properties using attributes, reducing the need for explicit validation logic in your controllers.

## Features

- Supports validation rules through attributes on model properties.
- Provides `[Validate]` and `[ValidateAsync]` attribute to automatically validate model objects in controller actions.
- Provides `[IInvalidValidationResponse]` interface for controlling invalid validation responses.
- Built on top of FluentValidation, so you still have the full power and flexibility of FluentValidation for more complex validation scenarios.

## Installation

You can install the library via NuGet Package Manager:

```bash
dotnet add package Ogu.FluentValidation.AspNetCore.Attributes
```
## Usage

**Controller:**
```csharp
[HttpPost]
[Validate(typeof(UserModel))]
public IActionResult Post([FromBody]UserModel userModel)
{
    ...
}
```

**Startup:**
```csharp
public void ConfigureServices(IServiceCollection services)
{
    ...
    services.AddScoped<IValidator<UserModel>, UserModelValidator>();
    ...
}
```

**UserModelValidator:**
```csharp
public class UserModelValidator : AbstractValidator<UserModel>
{
    public UserModelValidator()
    {
        RuleFor(u => u.Username).NotEmpty();
    }
}
```

**(optional):**
You may customize validation failure response by using default implemented `[IInvalidValidationResponse]` interface 
```csharp
public void ConfigureServices(IServiceCollection services)
{
    ...
    services.AddInvalidValidationResponse(failures => BadRequest(failures));
    ...
}
```

Or you can implement `[IInvalidValidationResponse]` interface
```csharp
public class MyCustomizedInvalidValidationResponse : IInvalidValidationResponse
{
    public IActionResult GetResult(object model, List<ValidationFailure> validationFailures)
    {
        return new BadRequestObjectResult(validationFailures);
    }

    public async Task<IActionResult> GetResultAsync(object model, List<ValidationFailure> validationFailures,
        CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(new BadRequestObjectResult(validationFailures));
    }
}
```
```csharp
public void ConfigureServices(IServiceCollection services)
{
    ...
    services.AddSingleton<IInvalidValidationResponse, MyCustomizedInvalidValidationResponse>();
    ...
}
```


