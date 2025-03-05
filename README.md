### **Email Validator - A Robust .NET Library for Email Validation**
The **Email Validator** is a powerful and easy-to-use .NET library designed to validate email addresses with precision and efficiency. It ensures that email addresses are not only syntactically correct but also checks for disposable or temporary email domains and verifies the existence of MX (Mail Exchange) records for the domain. With built-in support for caching, asynchronous operations, and third-party API integration, this library is perfect for applications that require reliable email validation.

---

### **Key Features**
- **Email Format Validation**: Validates email addresses using a robust regular expression to ensure they follow the correct format (e.g., `user@domain.com`).
- **Disposable Email Detection**: Detects and blocks emails from known disposable or temporary email services (e.g., Mailinator, 10MinuteMail) using a configurable list of domains.
- **Third-Party API Integration**: Seamlessly integrates with external APIs (e.g., Abstract API, Mailboxlayer) for enhanced disposable email detection and additional validation features.
- **MX Record Verification**: Checks if the domain of the email address has valid MX records, ensuring the domain can receive emails.
- **Caching for Performance**: Implements caching for MX record lookups to improve performance and reduce redundant DNS queries.
- **Asynchronous Support**: Provides asynchronous methods for non-blocking email validation, making it ideal for high-performance applications.
- **Customizable Configuration**: Allows users to configure the list of disposable email domains and other settings via a configuration file or programmatically.

---

### **Why Use Email Validator?**
- **Improve Data Quality**: Ensure that only valid and non-disposable email addresses are accepted in your application.
- **Enhance Security**: Reduce the risk of spam, fraud, and abuse by blocking temporary or disposable email addresses.
- **Boost Performance**: Optimize validation with caching and asynchronous operations for faster processing.
- **Easy Integration**: Simple API design and seamless integration with .NET applications.
- **Flexible and Extensible**: Customize validation rules and integrate with third-party services for advanced use cases.

---

### **Use Cases**
- **User Registration**: Validate email addresses during user sign-up to prevent fake or disposable accounts.
- **Newsletter Subscriptions**: Ensure only valid email addresses are added to your mailing list.
- **E-commerce**: Verify customer email addresses during checkout to reduce fraud and improve communication.
- **Lead Generation**: Validate email addresses collected from forms to maintain a high-quality lead database.
- **Data Cleaning**: Clean and validate email addresses in existing databases or CSV files.

---

### **Installation**
Install the **Email Validator** package via NuGet:
```bash
dotnet add package EmailValidator
```

---

### **Quick Start**
```csharp
using EmailValidator;

var email = "test@example.com";
if (await EmailValidator.ValidateEmailAsync(email))
{
    Console.WriteLine("Email is valid.");
}
else
{
    Console.WriteLine("Email is invalid or disposable.");
}
```

---

### **Configuration**
Configure the list of disposable email domains in `appsettings.json`:
```json
{
    "DisposableDomains": [
        "mailinator.com",
        "10minutemail.com",
        "guerrillamail.com"
    ]
}
```

---

### **Dependencies**
- **Microsoft.Extensions.Caching.Memory**: For caching MX record lookups.
- **System.Net.Http**: For making HTTP requests to third-party APIs.
- **Microsoft.Extensions.Configuration**: For loading configuration settings.

---

### **Contributing**
Contributions are welcome! If you’d like to contribute, please fork the repository and submit a pull request. For major changes, open an issue first to discuss your ideas.

---

### **License**
This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

---

### **Links**
- [NuGet Package](https://www.nuget.org/packages/EmailDomainValidator)
- [GitHub Repository](https://github.com/princejk/EmailDomainValidator)
- [Documentation](https://github.com/princejk/EmailDomainValidator/wiki)

---

This description is professional, concise, and highlights the value of your package. It’s designed to attract developers and businesses looking for a reliable email validation solution. Let me know if you’d like to tweak it further! 🚀
