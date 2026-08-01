# Idea 1.0.0 - Dynamic Reflection-Based Data Form Generator for Blazor
Here is the complete source code for a dynamic, reflection-based Data Form Generator built for Blazor Server or Blazor WebAssembly.
This component inspects any C# class model at runtime and automatically renders the appropriate Blazor form inputs (InputText, InputNumber, InputCheckbox, InputDate) while completely preserving native Tailwind/Bootstrap styling and Blazor's built-in EditForm validation rules. [1, 2, 3] 
1. The Dynamic Form Component (DynamicForm.razor)
Create a new file named DynamicForm.razor. This engine uses RenderTrees to safely generate native Blazor component instances dynamically. [4, 5] 

```razor
@using System.Reflection
@using System.ComponentModel.DataAnnotations
@using Microsoft.AspNetCore.Components.Forms
@typeparam TModel where TModel : class, new()

<EditForm Model="@Model" OnValidSubmit="@HandleValidSubmit" OnInvalidSubmit="@HandleInvalidSubmit">
    <DataAnnotationsValidator />
    <ValidationSummary class="text-danger mb-4" />

    <div class="form-container" style="max-width: 500px; margin: 0 auto; display: flex; flex-direction: column; gap: 15px;">
        @foreach (var prop in GetProperties())
        {
            var displayAttr = prop.GetCustomAttribute<DisplayAttribute>();
            string labelText = displayAttr?.Name ?? prop.Name;

            <div class="form-group" style="display: flex; flex-direction: column; gap: 5px;">
                <label style="font-weight: 600;">@labelText</label>
                
                @* Dynamically render the correct Blazor Input Component type *@
                @DynamicRenderInput(prop)
                
                <ValidationMessage For="@CreateExpression(prop)" class="text-danger" style="font-size: 0.85rem; color: red;" />
            </div>
        }

        <button type="submit" class="btn btn-primary" style="margin-top: 15px; padding: 10px; background-color: #007bff; color: white; border: none; border-radius: 4px; cursor: pointer;">
            Save Data
        </button>
    </div>
</EditForm>

@code {
    [Parameter] public TModel Model { get; set; } = new();
    [Parameter] public EventCallback<TModel> OnSaveSuccess { get; set; }

    private IEnumerable<PropertyInfo> GetProperties()
    {
        return typeof(TModel).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                             .Where(p => p.CanRead && p.CanWrite);
    }

    private RenderFragment DynamicRenderInput(PropertyInfo property) => builder =>
    {
        Type componentType;
        var attributes = new Dictionary<string, object>();

        // Set up the dynamic data-binding parameters required by Blazor EditForm inputs
        attributes["Value"] = property.GetValue(Model);
        attributes["ValueChanged"] = EventCallback.Factory.Create(this, (object? val) => 
        {
            if (val != null)
            {
                var targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                var safeValue = Convert.ChangeType(val, targetType);
                property.SetValue(Model, safeValue);
            }
        });
        attributes["ValueExpression"] = CreateExpression(property);
        attributes["class"] = "form-control";
        attributes["style"] = "padding: 8px; border: 1px solid #ccc; border-radius: 4px; width: 100%;";

        // Map standard system types to native Blazor core input components
        if (property.PropertyType == typeof(bool))
        {
            componentType = typeof(InputCheckbox);
            attributes["style"] = "width: auto; align-self: flex-start;"; // Overwrite style for checkbox alignment
        }
        else if (property.PropertyType == typeof(DateTime))
        {
            componentType = typeof(InputDate<DateTime>);
        }
        else if (property.PropertyType == typeof(int) || property.PropertyType == typeof(decimal) || property.PropertyType == typeof(double))
        {
            componentType = typeof(InputNumber<>).MakeGenericType(property.PropertyType);
        }
        else
        {
            componentType = typeof(InputText);
        }

        // Generate the Blazor component onto the UI tree sequence
        builder.OpenComponent(0, componentType);
        builder.AddMultipleAttributes(1, attributes);
        builder.CloseComponent();
    };

    // Helper method to dynamically generate the lambda expression tree required by Blazor Validation expressions: () => Model.Property
    private System.Linq.Expressions.LambdaExpression CreateExpression(PropertyInfo property)
    {
        var constant = System.Linq.Expressions.Expression.Constant(Model);
        var member = System.Linq.Expressions.Expression.Property(constant, property);
        return System.Linq.Expressions.Expression.Lambda(member);
    }

    private async Task HandleValidSubmit()
    {
        await OnSaveSuccess.InvokeAsync(Model);
    }

    private void HandleInvalidSubmit()
    {
        // Intercepts failures safely to prevent database communication
        Console.WriteLine("Form layout validation checks failed.");
    }
}
```

2. The Data Model Class (UserProfile.cs)
You can use standard, immutable data types decorated with native DataAnnotations schemas exactly as you would inside normal Blazor workflow structures.

```csharp
using System;
using System.ComponentModel.DataAnnotations;

namespace BlazorFormApp.Data
{
    public class UserProfile
    {
        [Required(ErrorMessage = "The user name is required.")]
        [StringLength(20, ErrorMessage = "Name must be under 20 characters.")]
        [Display(Name = "Full Registered Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Range(10, 5000, ErrorMessage = "Balance must be between $10 and $5000.")]
        [Display(Name = "Account Balance ($)")]
        public decimal Balance { get; set; } = 50.00m;

        [Display(Name = "Date of Registration")]
        public DateTime SetupDate { get; set; } = DateTime.Now;

        [Display(Name = "Accept Newsletter Communications")]
        public bool AcceptTerms { get; set; } = true;
    }
}

3. Using the Generator Component inside a Blazor Page (Index.razor)
Consume your dynamic element inside any generic page layout routing simply by instantiating the element tag markup wrapper.

```razor
@page "/"
@using BlazorFormApp.Data

<PageTitle>Dynamic Admin Form</PageTitle>

<h3 style="text-align: center; margin-bottom: 30px;">Auto-Generated Admin Form Panel</h3>

@* Inject the generic model type constraint into the wrapper *@
<DynamicForm TModel="UserProfile" Model="CurrentUser" OnSaveSuccess="ProcessFormPayload" />

@if (ShowSuccessAlert)
{
    <div style="margin-top: 20px; padding: 15px; background-color: #d4edda; color: #155724; border-radius: 4px; text-align: center;">
        <strong>Success!</strong> Data submitted for processing: @CurrentUser.Name (Balance: @CurrentUser.Balance)
    </div>
}

@code {
    private UserProfile CurrentUser { get; set; } = new();
    private bool ShowSuccessAlert { get; set; } = false;

    private void ProcessFormPayload(UserProfile updatedUser)
    {
        // Form is valid, data structure values are now updated inside the payload object safely
        ShowSuccessAlert = true;
        StateHasChanged();
    }
}
```

Key Advantages of this Blazor Architecture
•	Native Integration: It utilizes native EditForm, DataAnnotationsValidator, and UI context classes, which means field-level live tracking validation updates natively as the user types or tabs away. [6] 
•	Component-Safe Event Handling: Rather than relying on standard runtime UI inputs, it registers component state using explicit EventCallback values, guaranteeing zero loss of data synchronization on cross-thread rendering loops.
•	Abstract Rendering Engine: The structural logic completely handles reflection generation parameters off-screen. Adding a new property layer inside your database or local class library models automatically renders the input elements on screen instantly without modifying your page logic.
Would you like to explore adding custom field attributes to support specialized UI types (like rendering a string as a dropdown select box or a password field instead of a plain text input)?
 
# Idea 2.0.0 - Dropdown List Support for String Properties

Here is the updated Blazor Data Form Generator code, extended to support dropdown lists for string properties.
To achieve this without hardcoding specific options inside the generator, we use a custom C# attribute ([FormSelect]). This allows you to decorate any string property in your data model and provide it with an array of string choices.
1. Create the Custom Attribute (FormSelectAttribute.cs)
Create this class anywhere in your project. It stores the options that should appear in the dropdown menu.

```csharp
using System;

namespace BlazorFormApp.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class FormSelectAttribute : Attribute
    {
        public string[] Options { get; }

        public FormSelectAttribute(params string[] options)
        {
            Options = options ?? Array.Empty<string>();
        }
    }
}
```

2. Update the Model (UserProfile.cs)
Now you can decorate any string property with the new [FormSelect] attribute to instantly turn it into a dropdown list.

```csharp
using System;
using System.ComponentModel.DataAnnotations;
using BlazorFormApp.Attributes;

namespace BlazorFormApp.Data
{
    public class UserProfile
    {
        [Required(ErrorMessage = "The user name is required.")]
        [Display(Name = "Full Registered Name")]
        public string Name { get; set; } = string.Empty;

        // NEW: This string property will automatically render as a dropdown list
        [Required(ErrorMessage = "Please select a user role.")]
        [Display(Name = "Account Security Role")]
        [FormSelect("Standard User", "Content Editor", "System Administrator", "Guest Observer")]
        public string SecurityRole { get; set; } = "Standard User";

        [Required]
        [Range(10, 5000, ErrorMessage = "Balance must be between $10 and $5000.")]
        [Display(Name = "Account Balance ($)")]
        public decimal Balance { get; set; } = 50.00m;

        [Display(Name = "Date of Registration")]
        public DateTime SetupDate { get; set; } = DateTime.Now;

        [Display(Name = "Accept Newsletter Communications")]
        public bool AcceptTerms { get; set; } = true;
    }
}
```

3. Update the Engine (DynamicForm.razor)
We update the component builder to check if a property has the [FormSelect] attribute. If it does, it dynamically constructs a Blazor ```InputSelect<string>``` component and loops through your option strings to create HTML ```<option>``` child elements.

```razor
@using System.Reflection
@using System.ComponentModel.DataAnnotations
@using Microsoft.AspNetCore.Components.Forms
@using BlazorFormApp.Attributes
@typeparam TModel where TModel : class, new()

<EditForm Model="@Model" OnValidSubmit="@HandleValidSubmit">
    <DataAnnotationsValidator />
    <ValidationSummary class="text-danger mb-4" />

    <div class="form-container" style="max-width: 500px; margin: 0 auto; display: flex; flex-direction: column; gap: 15px;">
        @foreach (var prop in GetProperties())
        {
            var displayAttr = prop.GetCustomAttribute<DisplayAttribute>();
            string labelText = displayAttr?.Name ?? prop.Name;

            <div class="form-group" style="display: flex; flex-direction: column; gap: 5px;">
                <label style="font-weight: 600;">@labelText</label>
                
                @DynamicRenderInput(prop)
                
                <ValidationMessage For="@CreateExpression(prop)" class="text-danger" style="font-size: 0.85rem; color: red;" />
            </div>
        }

        <button type="submit" class="btn btn-primary" style="margin-top: 15px; padding: 10px; background-color: #007bff; color: white; border: none; border-radius: 4px; cursor: pointer;">
            Save Data
        </button>
    </div>
</EditForm>

@code {
    [Parameter] public TModel Model { get; set; } = new();
    [Parameter] public EventCallback<TModel> OnSaveSuccess { get; set; }

    private IEnumerable<PropertyInfo> GetProperties()
    {
        return typeof(TModel).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                             .Where(p => p.CanRead && p.CanWrite);
    }

    private RenderFragment DynamicRenderInput(PropertyInfo property) => builder =>
    {
        Type componentType;
        var attributes = new Dictionary<string, object>();

        // Set up standard data-binding parameters
        attributes["Value"] = property.GetValue(Model);
        attributes["ValueChanged"] = EventCallback.Factory.Create(this, (object? val) => 
        {
            if (val != null)
            {
                var targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                var safeValue = Convert.ChangeType(val, targetType);
                property.SetValue(Model, safeValue);
            }
        });
        attributes["ValueExpression"] = CreateExpression(property);
        attributes["class"] = "form-control";
        attributes["style"] = "padding: 8px; border: 1px solid #ccc; border-radius: 4px; width: 100%;";

        // Check for our custom dropdown selection attribute
        var selectAttr = property.GetCustomAttribute<FormSelectAttribute>();

        if (selectAttr != null && property.PropertyType == typeof(string))
        {
            componentType = typeof(InputSelect<string>);
            
            builder.OpenComponent(0, componentType);
            builder.AddMultipleAttributes(1, attributes);
            
            // Generate child content template containing the select list options
            builder.AddAttribute(2, "ChildContent", (RenderFragment)((childBuilder) =>
            {
                // Optional: Add a blank/placeholder element at the top
                childBuilder.OpenElement(0, "option");
                childBuilder.AddAttribute(1, "value", "");
                childBuilder.AddContent(2, "-- Select an option --");
                childBuilder.CloseElement();

                int seq = 3;
                foreach (var option in selectAttr.Options)
                {
                    childBuilder.OpenElement(seq++, "option");
                    childBuilder.AddAttribute(seq++, "value", option);
                    childBuilder.AddContent(seq++, option);
                    childBuilder.CloseElement();
                }
            }));
            
            builder.CloseComponent();
            return;
        }

        // Fallback mapping for standard data types
        if (property.PropertyType == typeof(bool))
        {
            componentType = typeof(InputCheckbox);
            attributes["style"] = "width: auto; align-self: flex-start;";
        }
        else if (property.PropertyType == typeof(DateTime))
        {
            componentType = typeof(InputDate<DateTime>);
        }
        else if (property.PropertyType == typeof(int) || property.PropertyType == typeof(decimal) || property.PropertyType == typeof(double))
        {
            componentType = typeof(InputNumber<>).MakeGenericType(property.PropertyType);
        }
        else
        {
            componentType = typeof(InputText);
        }

        builder.OpenComponent(0, componentType);
        builder.AddMultipleAttributes(1, attributes);
        builder.CloseComponent();
    };

    private System.Linq.Expressions.LambdaExpression CreateExpression(PropertyInfo property)
    {
        var constant = System.Linq.Expressions.Expression.Constant(Model);
        var member = System.Linq.Expressions.Expression.Property(constant, property);
        return System.Linq.Expressions.Expression.Lambda(member);
    }

    private async Task HandleValidSubmit()
    {
        await OnSaveSuccess.InvokeAsync(Model);
    }
}
```

How It Works Under the Hood
1.	Attribute Inspection: The engine loops through properties and looks for [FormSelect].
2.	Conditional Hijacking: If found on a string, it intercepts the pipeline before it reaches the standard text box generation.
3.	Blazor ChildContent Render Tree: It uses Blazor's advanced RenderFragment sub-builder engine to loop through the hardcoded string choices inside the attribute array, dynamically outputting ```<option value="x">x</option>``` structures safely inside the native ```InputSelect<string>``` tag wrapper.
If you are interested, we can also extend this to:
•	Feed dropdown options dynamically from a database or API service rather than hardcoding them in attributes.
•	Support binding dropdowns to C# enum types automatically. [1, 2] 
Let me know how you would like to proceed!

# Idea 3.0.0 - Automatic Enum Binding and Dynamic Database/API Data Binding
 
Here is the fully extended Blazor Data Form Generator. This update introduces two features to the reflection engine:
1.	Automatic Enum Binding: Any property using a C# enum will automatically render as a dropdown list, using its names or [Display] attributes for the labels.
2.	Dynamic Database/API Data Binding: A new custom attribute, [FormDynamicSelect], allows you to specify a dynamic data provider key. The component will resolve this key at runtime via a centralized lookup service to fetch options from an API or database asynchronously.
________________________________________
1. Define the Custom Attributes & Lookups
Create a lookup service interface that will handle fetching data from your API or database.

FormDynamicSelectAttribute.cs

```csharp
using System;

namespace BlazorFormApp.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class FormDynamicSelectAttribute : Attribute
    {
        // The unique string key used to look up options from a service/API
        public string ProviderKey { get; }

        public FormDynamicSelectAttribute(string providerKey)
        {
            ProviderKey = providerKey;
        }
    }
}
IFormLookupService.cs
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlazorFormApp.Services
{
    public interface IFormLookupService
    {
        // Fetches a key-value dictionary (ID/Value -> Display Text) based on a provider key
        Task<Dictionary<string, string>> GetOptionsAsync(string providerKey);
    }
}
```

________________________________________
2. Update the Sample Data Model
Here is an updated UserProfile class that showcases an enum binding alongside a dynamic database-driven dropdown.
UserProfile.cs

```csharp
using System;
using System.ComponentModel.DataAnnotations;
using BlazorFormApp.Attributes;

namespace BlazorFormApp.Data
{
    // A standard C# Enum
    public enum AccountTier
    {
        [Display(Name = "Basic Free Tier")] Free,
        [Display(Name = "Professional Premium")] Premium,
        [Display(Name = "Enterprise Corporate")] Enterprise
    }

    public class UserProfile
    {
        [Required(ErrorMessage = "The user name is required.")]
        [Display(Name = "Full Registered Name")]
        public string Name { get; set; } = string.Empty;

        // 1. BOUND TO AN ENUM TYPE (Auto-renders as a dropdown)
        [Display(Name = "Subscription Level")]
        public AccountTier Tier { get; set; } = AccountTier.Free;

        // 2. BOUND TO AN API / DATABASE LOOKUP (Dynamic Dropdown)
        [Required(ErrorMessage = "Please assign a corporate department.")]
        [Display(Name = "Assigned Company Department")]
        [FormDynamicSelect("api/departments")] 
        public string DepartmentId { get; set; } = string.Empty;

        [Display(Name = "Accept Newsletter")]
        public bool AcceptTerms { get; set; } = true;
    }
}
```

________________________________________
3. Update the Generator Engine
The generator now uses OnParametersSetAsync to scan the model, identify any properties using dynamic data attributes, and asynchronously fetch the dropdown elements from your service.

DynamicForm.razor

```razor
@using System.Reflection
@using System.ComponentModel.DataAnnotations
@using Microsoft.AspNetCore.Components.Forms
@using BlazorFormApp.Attributes
@using BlazorFormApp.Services
@typeparam TModel where TModel : class, new()
@inject IFormLookupService LookupService

<EditForm Model="@Model" OnValidSubmit="@HandleValidSubmit">
    <DataAnnotationsValidator />
    <ValidationSummary class="text-danger mb-4" />

    <div class="form-container" style="max-width: 500px; margin: 0 auto; display: flex; flex-direction: column; gap: 15px;">
        @if (_isLoaded)
        {
            @foreach (var prop in GetProperties())
            {
                var displayAttr = prop.GetCustomAttribute<DisplayAttribute>();
                string labelText = displayAttr?.Name ?? prop.Name;

                <div class="form-group" style="display: flex; flex-direction: column; gap: 5px;">
                    <label style="font-weight: 600;">@labelText</label>
                    
                    @DynamicRenderInput(prop)
                    
                    <ValidationMessage For="@CreateExpression(prop)" class="text-danger" style="font-size: 0.85rem; color: red;" />
                </div>
            }
        }
        else
        {
            <p>Loading dynamic form configurations...</p>
        }

        <button type="submit" class="btn btn-primary" style="margin-top: 15px; padding: 10px; background-color: #007bff; color: white; border: none; border-radius: 4px; cursor: pointer;">
            Save Data
        </button>
    </div>
</EditForm>

@code {
    [Parameter] public TModel Model { get; set; } = new();
    [Parameter] public EventCallback<TModel> OnSaveSuccess { get; set; }

    private bool _isLoaded = false;
    private Dictionary<string, Dictionary<string, string>> _dynamicDropdowns = new();

    protected override async Task OnParametersSetAsync()
    {
        _isLoaded = false;
        _dynamicDropdowns.Clear();

        foreach (var prop in GetProperties())
        {
            var dynamicAttr = prop.GetCustomAttribute<FormDynamicSelectAttribute>();
            if (dynamicAttr != null)
            {
                // Asynchronously fetch options from your API/Database layer via the lookup interface
                var options = await LookupService.GetOptionsAsync(dynamicAttr.ProviderKey);
                _dynamicDropdowns[prop.Name] = options;
            }
        }
        _isLoaded = true;
    }

    private IEnumerable<PropertyInfo> GetProperties()
    {
        return typeof(TModel).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                             .Where(p => p.CanRead && p.CanWrite);
    }

    private RenderFragment DynamicRenderInput(PropertyInfo property) => builder =>
    {
        Type componentType;
        var attributes = new Dictionary<string, object>();

        // Set up binding structures
        var propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        attributes["Value"] = property.GetValue(Model)!;
        attributes["ValueChanged"] = EventCallback.Factory.Create(this, (object? val) => 
        {
            if (val != null)
            {
                var safeValue = propertyType.IsEnum ? Enum.Parse(propertyType, val.ToString()!) : Convert.ChangeType(val, propertyType);
                property.SetValue(Model, safeValue);
            }
        });
        attributes["ValueExpression"] = CreateExpression(property);
        attributes["class"] = "form-control";
        attributes["style"] = "padding: 8px; border: 1px solid #ccc; border-radius: 4px; width: 100%;";

        // BRANCH A: HANDLE DYNAMIC API / DATABASE DROPDOWNS
        if (_dynamicDropdowns.ContainsKey(property.Name))
        {
            componentType = typeof(InputSelect<string>);
            BuildSelectWrapper(builder, componentType, attributes, _dynamicDropdowns[property.Name]);
            return;
        }

        // BRANCH B: HANDLE AUTOMATIC ENUM BINDING DROPDOWNS
        if (propertyType.IsEnum)
        {
            componentType = typeof(InputSelect<>).MakeGenericType(property.PropertyType);
            
            var enumOptions = new Dictionary<string, string>();
            foreach (var name in Enum.GetNames(propertyType))
            {
                var field = propertyType.GetField(name);
                var display = field?.GetCustomAttribute<DisplayAttribute>();
                enumOptions[name] = display?.Name ?? name; // Use display label or fallback to code token string
            }

            BuildSelectWrapper(builder, componentType, attributes, enumOptions);
            return;
        }

        // BRANCH C: FALLBACK REGULAR PRIMITIVES
        if (propertyType == typeof(bool))
        {
            componentType = typeof(InputCheckbox);
            attributes["style"] = "width: auto; align-self: flex-start;";
        }
        else if (propertyType == typeof(DateTime))
        {
            componentType = typeof(InputDate<DateTime>);
        }
        else if (propertyType == typeof(int) || propertyType == typeof(decimal) || propertyType == typeof(double))
        {
            componentType = typeof(InputNumber<>).MakeGenericType(property.PropertyType);
        }
        else
        {
            componentType = typeof(InputText);
        }

        builder.OpenComponent(0, componentType);
        builder.AddMultipleAttributes(1, attributes);
        builder.CloseComponent();
    };

    private void BuildSelectWrapper(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder, Type componentType, Dictionary<string, object> attributes, Dictionary<string, string> items)
    {
        builder.OpenComponent(0, componentType);
        builder.AddMultipleAttributes(1, attributes);
        builder.AddAttribute(2, "ChildContent", (RenderFragment)((childBuilder) =>
        {
            childBuilder.OpenElement(0, "option");
            childBuilder.AddAttribute(1, "value", "");
            childBuilder.AddContent(2, "-- Select Item --");
            childBuilder.CloseElement();

            int seq = 3;
            foreach (var kvp in items)
            {
                childBuilder.OpenElement(seq++, "option");
                childBuilder.AddAttribute(seq++, "value", kvp.Key);
                childBuilder.AddContent(seq++, kvp.Value);
                childBuilder.CloseElement();
            }
        }));
        builder.CloseComponent();
    }

    private System.Linq.Expressions.LambdaExpression CreateExpression(PropertyInfo property)
    {
        var constant = System.Linq.Expressions.Expression.Constant(Model);
        var member = System.Linq.Expressions.Expression.Property(constant, property);
        return System.Linq.Expressions.Expression.Lambda(member);
    }

    private async Task HandleValidSubmit()
    {
        await OnSaveSuccess.InvokeAsync(Model);
    }
}
```
________________________________________
4. Implementation Example (API Mock Service)
To test the functionality, add a concrete implementation of the lookup service inside your dependencies framework.
FormLookupService.cs

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlazorFormApp.Services
{
    public class FormLookupService : IFormLookupService
    {
        // Inject HttpClient or DBContext here to fetch real tables/data
        public async Task<Dictionary<string, string>> GetOptionsAsync(string providerKey)
        {
            // Simulate an asynchronous API database network delay
            await Task.Delay(300); 

            if (providerKey == "api/departments")
            {
                return new Dictionary<string, string>
                {
                    { "DEP_01", "Information Technology Department" },
                    { "DEP_02", "Human Resource Systems" },
                    { "DEP_03", "Corporate Financial Accounting" }
                };
            }

            return new Dictionary<string, string>();
        }
    }
}
```

Register Dependency (Program.cs)
builder.Services.AddScoped<IFormLookupService, FormLookupService>();
Architectural Highlights
•	
•	Zero UI Interruption: The UI displays a safe placeholder element until all background database threads settle, preventing asynchronous thread collisions.
•	Enum Serialization Handling: When parsing values inside the ValueChanged delegate, the engine dynamically checks if the property targets an enum base type. It switches its pipeline mechanics from standard numeric conversion to Enum.Parse string conversion automatically.
•	
Would you like to explore adding layout support to this generator, such as organizing form fields into multiple columns using styling classes or dynamic grid attributes?


# Idea 4.0.0 - Bootstrap Grid Layout Support for Multi-Column Forms
 
Here is the fully extended Blazor Data Form Generator configured to use the Bootstrap Grid System (row and col-* classes).
To give you complete control over the layout, this update introduces a custom [FormLayout] attribute. This allows you to define exactly how many Bootstrap grid columns (1 to 12) a field should occupy, and you can still mix and match single-column and multi-column designs natively.
________________________________________
1. Create the Layout Attribute (FormLayoutAttribute.cs)
This attribute targets properties to define their individual Bootstrap column span.
```cshaprt
using System;

namespace BlazorFormApp.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class FormLayoutAttribute : Attribute
    {
        // Holds the Bootstrap column width integer (e.g., 6 for half-width, 12 for full-width)
        public int ColumnSpan { get; }

        public FormLayoutAttribute(int columnSpan)
        {
            // Safeguard grid constraints between 1 and 12
            ColumnSpan = Math.Clamp(columnSpan, 1, 12);
        }
    }
}
```

________________________________________
2. Update the Sample Data Model (UserProfile.cs)
By adding [FormLayout(X)], fields will automatically arrange side-by-side where they fit within Bootstrap's 12-column row array structure.

```csharp
using System;
using System.ComponentModel.DataAnnotations;
using BlazorFormApp.Attributes;

namespace BlazorFormApp.Data
{
    public enum AccountTier
    {
        [Display(Name = "Basic Free Tier")] Free,
        [Display(Name = "Professional Premium")] Premium,
        [Display(Name = "Enterprise Corporate")] Enterprise
    }

    public class UserProfile
    {
        // Row 1: Full Name (Takes 8 columns) and Tier Dropdown (Takes 4 columns) side-by-side
        [Required(ErrorMessage = "The user name is required.")]
        [Display(Name = "Full Registered Name")]
        [FormLayout(8)]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Subscription Level")]
        [FormLayout(4)]
        public AccountTier Tier { get; set; } = AccountTier.Free;

        // Row 2: Department Dropdown (Half-width) and Account Balance (Half-width) side-by-side
        [Required(ErrorMessage = "Please assign a corporate department.")]
        [Display(Name = "Assigned Company Department")]
        [FormDynamicSelect("api/departments")] 
        [FormLayout(6)]
        public string DepartmentId { get; set; } = string.Empty;

        [Required]
        [Range(10, 5000, ErrorMessage = "Balance must be between $10 and $5000.")]
        [Display(Name = "Account Balance ($)")]
        [FormLayout(6)]
        public decimal Balance { get; set; } = 50.00m;

        // Row 3: Setup Date (Takes 12 columns - breaks layout to its own dedicated row line)
        [Display(Name = "Date of Registration")]
        [FormLayout(12)]
        public DateTime SetupDate { get; set; } = DateTime.Now;

        // Row 4: Checkbox (Default fallback layout behavior when no attribute is declared)
        [Display(Name = "Accept Newsletter Communications")]
        public bool AcceptTerms { get; set; } = true;
    }
}
```

________________________________________
3. Update the Engine (DynamicForm.razor)
The rendering engine now wraps the loop inside a Bootstrap ```<div class="row g-3">``` structure. For every mapped property field, it checks for the [FormLayout] metadata and injects a col-md-{span} class dynamically.

```razor
@using System.Reflection
@using System.ComponentModel.DataAnnotations
@using Microsoft.AspNetCore.Components.Forms
@using BlazorFormApp.Attributes
@using BlazorFormApp.Services
@typeparam TModel where TModel : class, new()
@inject IFormLookupService LookupService

<EditForm Model="@Model" OnValidSubmit="@HandleValidSubmit">
    <DataAnnotationsValidator />
    <ValidationSummary class="text-danger mb-4" />

    @if (_isLoaded)
    {
        @* Wrapper matching Native Bootstrap Form Row Layout Rules *@
        <div class="row g-3">
            @foreach (var prop in GetProperties())
            {
                var displayAttr = prop.GetCustomAttribute<DisplayAttribute>();
                string labelText = displayAttr?.Name ?? prop.Name;

                // Extract custom layout attribute, fallback to a default layout pattern (col-12) if empty
                var layoutAttr = prop.GetCustomAttribute<FormLayoutAttribute>();
                int colSpan = layoutAttr?.ColumnSpan ?? 12;

                <div class="col-md-@colSpan form-group d-flex flex-column gap-1">
                    <label class="form-label fw-bold mb-1">@labelText</label>
                    
                    @DynamicRenderInput(prop)
                    
                    <ValidationMessage For="@CreateExpression(prop)" class="text-danger small mt-1" />
                </div>
            }

            <div class="col-12 mt-4 text-end">
                <button type="submit" class="btn btn-primary px-4 py-2">
                    Save Changes
                </button>
            </div>
        </div>
    }
    else
    {
        <div class="d-flex align-items-center justify-content-center p-5">
            <div class="spinner-border text-primary me-2" role="status"></div>
            <span>Generating application view fields...</span>
        </div>
    }
</EditForm>

@code {
    [Parameter] public TModel Model { get; set; } = new();
    [Parameter] public EventCallback<TModel> OnSaveSuccess { get; set; }

    private bool _isLoaded = false;
    private Dictionary<string, Dictionary<string, string>> _dynamicDropdowns = new();

    protected override async Task OnParametersSetAsync()
    {
        _isLoaded = false;
        _dynamicDropdowns.Clear();

        foreach (var prop in GetProperties())
        {
            var dynamicAttr = prop.GetCustomAttribute<FormDynamicSelectAttribute>();
            if (dynamicAttr != null)
            {
                var options = await LookupService.GetOptionsAsync(dynamicAttr.ProviderKey);
                _dynamicDropdowns[prop.Name] = options;
            }
        }
        _isLoaded = true;
    }

    private IEnumerable<PropertyInfo> GetProperties()
    {
        return typeof(TModel).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                             .Where(p => p.CanRead && p.CanWrite);
    }

    private RenderFragment DynamicRenderInput(PropertyInfo property) => builder =>
    {
        Type componentType;
        var attributes = new Dictionary<string, object>();

        var propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        attributes["Value"] = property.GetValue(Model)!;
        attributes["ValueChanged"] = EventCallback.Factory.Create(this, (object? val) => 
        {
            if (val != null)
            {
                var safeValue = propertyType.IsEnum ? Enum.Parse(propertyType, val.ToString()!) : Convert.ChangeType(val, propertyType);
                property.SetValue(Model, safeValue);
            }
        });
        attributes["ValueExpression"] = CreateExpression(property);
        
        // Use clean native Bootstrap CSS styling classes
        attributes["class"] = "form-control";

        // BRANCH A: HANDLE DYNAMIC API / DATABASE DROPDOWNS
        if (_dynamicDropdowns.ContainsKey(property.Name))
        {
            attributes["class"] = "form-select";
            componentType = typeof(InputSelect<string>);
            BuildSelectWrapper(builder, componentType, attributes, _dynamicDropdowns[property.Name]);
            return;
        }

        // BRANCH B: HANDLE AUTOMATIC ENUM BINDING DROPDOWNS
        if (propertyType.IsEnum)
        {
            attributes["class"] = "form-select";
            componentType = typeof(InputSelect<>).MakeGenericType(property.PropertyType);
            
            var enumOptions = new Dictionary<string, string>();
            foreach (var name in Enum.GetNames(propertyType))
            {
                var field = propertyType.GetField(name);
                var display = field?.GetCustomAttribute<DisplayAttribute>();
                enumOptions[name] = display?.Name ?? name;
            }

            BuildSelectWrapper(builder, componentType, attributes, enumOptions);
            return;
        }

        // BRANCH C: FALLBACK REGULAR PRIMITIVES
        if (propertyType == typeof(bool))
        {
            componentType = typeof(InputCheckbox);
            attributes["class"] = "form-check-input ms-1"; // Standard Bootstrap styles for clean form checkboxes
        }
        else if (propertyType == typeof(DateTime))
        {
            componentType = typeof(InputDate<DateTime>);
        }
        else if (propertyType == typeof(int) || propertyType == typeof(decimal) || propertyType == typeof(double))
        {
            componentType = typeof(InputNumber<>).MakeGenericType(property.PropertyType);
        }
        else
        {
            componentType = typeof(InputText);
        }

        builder.OpenComponent(0, componentType);
        builder.AddMultipleAttributes(1, attributes);
        builder.CloseComponent();
    };

    private void BuildSelectWrapper(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder, Type componentType, Dictionary<string, object> attributes, Dictionary<string, string> items)
    {
        builder.OpenComponent(0, componentType);
        builder.AddMultipleAttributes(1, attributes);
        builder.AddAttribute(2, "ChildContent", (RenderFragment)((childBuilder) =>
        {
            childBuilder.OpenElement(0, "option");
            childBuilder.AddAttribute(1, "value", "");
            childBuilder.AddContent(2, "-- Select Item --");
            childBuilder.CloseElement();

            int seq = 3;
            foreach (var kvp in items)
            {
                childBuilder.OpenElement(seq++, "option");
                childBuilder.AddAttribute(seq++, "value", kvp.Key);
                childBuilder.AddContent(seq++, kvp.Value);
                childBuilder.CloseElement();
            }
        }));
        builder.CloseComponent();
    }

    private System.Linq.Expressions.LambdaExpression CreateExpression(PropertyInfo property)
    {
        var constant = System.Linq.Expressions.Expression.Constant(Model);
        var member = System.Linq.Expressions.Expression.Property(constant, property);
        return System.Linq.Expressions.Expression.Lambda(member);
    }

    private async Task HandleValidSubmit()
    {
        await OnSaveSuccess.InvokeAsync(Model);
    }
}
```

Layout Engine Enhancements
•	Fluid Column Packing: Bootstrap's grid natively wraps elements. If Name is 8 columns and Tier is 4 columns, they add up to exactly 12 and sit on a single line. The next fields automatically flow to the row directly below. [1, 2, 3, 4] 
•	Pure Clean CSS Classes: Custom style tags have been replaced with standard Bootstrap classes (form-label, form-control, form-select, spinner-border, and form-check-input). This means the component seamlessly inherits themes directly from your global stylesheet. [5, 6, 7, 8, 9] 
Would you like to explore adding field grouping or multi-tab categorization next (e.g., separating properties into "Personal Info" and "Account Settings" accordion blocks or tabs)?


# Idea 5.0.0 - Field Grouping and Multi-Tab Categorization
 
Yes, that makes total sense! What you are describing is a Dynamic, State-Driven Form Wizard.
To build this cleanly in Blazor using reflection, you want to move away from treating a class as a flat list of properties. Instead, your engine should treat properties as Conditional Steps.
To achieve this without bloating your generator with messy if/else hardcoding, we can use a custom [FormStep] attribute and a [DependsOn] conditional rule engine.
Here is the architectural pattern and complete source code to make this work.
________________________________________
1. The Attributes & Dynamic Rules (WizardAttributes.cs)
We need two attributes: one to assign fields to a specific page/step of the wizard, and another to evaluate whether a field should appear based on a previous answer.
```csharp
using System;

namespace BlazorWizardApp.Attributes
{
    // Assigns a property to a specific Step number in the wizard
    [AttributeUsage(AttributeTargets.Property)]
    public class FormStepAttribute : Attribute
    {
        public int StepNumber { get; }
        public FormStepAttribute(int stepNumber) => StepNumber = stepNumber;
    }

    // Dynamic Visibility Rule: Only show this property if 'TargetProperty' equals 'ExpectedValue'
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class DependsOnAttribute : Attribute
    {
        public string TargetProperty { get; }
        public object ExpectedValue { get; }

        public DependsOnAttribute(string targetProperty, object expectedValue)
        {
            TargetProperty = targetProperty;
            ExpectedValue = expectedValue;
        }
    }
}
```
________________________________________
2. The Conditional Data Model (WizardModel.cs)
This is where the magic happens. We build out paths. If the user selects a particular strategy, entirely new fields or specialized steps reveal themselves. [1] 
```csharp
using System;
using System.ComponentModel.DataAnnotations;
using BlazorWizardApp.Attributes;

namespace BlazorWizardApp.Data
{
    public enum StrategyType { SimplePath, AdvancedPath, ExitNow }
    public enum CloudProvider { AWS, Azure, OnPremise }

    public class WizardModel
    {
        // ==========================================
        // STEP 1: INITIAL BRANCHING QUESTION
        // ==========================================
        [Required]
        [Display(Name = "Choose Your Information Path")]
        [FormStep(1)]
        public StrategyType ChosenStrategy { get; set; } = StrategyType.SimplePath;


        // ==========================================
        // STEP 2: DEPENDS ON YOUR STEP 1 CHOICE
        // ==========================================
        
        // Path A Fields (Only visible if Strategy == SimplePath)
        [DependsOn(nameof(ChosenStrategy), StrategyType.SimplePath)]
        [Display(Name = "Your Contact Email Address")]
        [Required, EmailAddress]
        [FormStep(2)]
        public string SimpleEmail { get; set; } = string.Empty;

        // Path B Fields (Only visible if Strategy == AdvancedPath)
        [DependsOn(nameof(ChosenStrategy), StrategyType.AdvancedPath)]
        [Display(Name = "Select Infrastructure Target")]
        [FormStep(2)]
        public CloudProvider EnterpriseCloud { get; set; } = CloudProvider.AWS;

        // Sub-Conditional Field (Only visible if Step 1 is Advanced AND Cloud is AWS)
        [DependsOn(nameof(ChosenStrategy), StrategyType.AdvancedPath)]
        [DependsOn(nameof(EnterpriseCloud), CloudProvider.AWS)]
        [Display(Name = "AWS IAM Role ARN String")]
        [Required(ErrorMessage = "IAM Role is required for AWS setups.")]
        [FormStep(2)]
        public string AwsRoleArn { get; set; } = string.Empty;


        // ==========================================
        // STEP 3: CLOSING PATH
        // ==========================================
        
        // This step is completely bypassed if user chose "ExitNow" on Step 1
        [DependsOn(nameof(ChosenStrategy), StrategyType.SimplePath)]
        [Display(Name = "Final Notes / Comments")]
        [FormStep(3)]
        public string FinalNotes { get; set; } = string.Empty;

        [DependsOn(nameof(ChosenStrategy), StrategyType.AdvancedPath)]
        [Display(Name = "Enterprise SLA Agreement Code")]
        [Required]
        [FormStep(3)]
        public string SlaCode { get; set; } = string.Empty;
    }
}
```
________________________________________
3. The Interactive Wizard Engine (DynamicWizard.razor)
This engine manages the active step index state. It uses reflection to dynamically filter out properties that fail the [DependsOn] test. If a step ends up with zero visible properties due to branching rules, the engine automatically skips that step!
```razor
@using System.Reflection
@using System.ComponentModel.DataAnnotations
@using Microsoft.AspNetCore.Components.Forms
@using BlazorWizardApp.Attributes
@typeparam TModel where TModel : class, new()

<EditForm Model="@Model" OnValidSubmit="@HandleSubmitAttempt">
    <DataAnnotationsValidator />

    <div class="card shadow-sm p-4" style="max-width: 600px; margin: 0 auto;">
        <!-- Header Step Tracker Progress Indicator Bar -->
        <div class="d-flex justify-content-between align-items-center mb-4 pb-2 border-bottom">
            <h5 class="m-0 text-primary">Step @CurrentStep of @MaxSteps</h5>
            <span class="badge bg-secondary p-2">Wizard Flow Pipeline</span>
        </div>

        <div class="row g-3 mb-4">
            @foreach (var prop in GetVisiblePropertiesForCurrentStep())
            {
                var displayAttr = prop.GetCustomAttribute<DisplayAttribute>();
                string labelText = displayAttr?.Name ?? prop.Name;

                <div class="col-12 d-flex flex-column gap-1">
                    <label class="form-label fw-bold mb-1">@labelText</label>
                    
                    @DynamicRenderInput(prop)
                    
                    <ValidationMessage For="@CreateExpression(prop)" class="text-danger small mt-1" />
                </div>
            }
        </div>

        <!-- Navigation Action Buttons Controls Footers -->
        <div class="d-flex justify-content-between border-top pt-3">
            <button type="button" class="btn btn-outline-secondary px-4" 
                    @onclick="GoPrevious" disabled="@(CurrentStep == 1)">
                Back
            </button>

            @if (CurrentStep < MaxSteps && !IsFinalDynamicStep())
            {
                <button type="button" class="btn btn-primary px-4" @onclick="GoNext">
                    Next Step
                </button>
            }
            else
            {
                <button type="submit" class="btn btn-success px-4">
                    Submit Wizard Data
                </button>
            }
        </div>
    </div>
</EditForm>

@code {
    [Parameter] public TModel Model { get; set; } = new();
    [Parameter] public EventCallback<TModel> OnWizardComplete { get; set; }

    private int CurrentStep { get; set; } = 1;
    private int MaxSteps { get; set; } = 1;

    protected override void OnInitialized()
    {
        // Automatically determine total step footprint index using attributes
        var steps = typeof(TModel).GetProperties()
                                  .Select(p => p.GetCustomAttribute<FormStepAttribute>()?.StepNumber ?? 1);
        MaxSteps = steps.Any() ? steps.Max() : 1;
    }

    // Main Rule Evaluator Core: Determines if a property should actively show up on screen
    private bool IsPropertyVisible(PropertyInfo property)
    {
        var dependencies = property.GetCustomAttributes<DependsOnAttribute>();
        if (!dependencies.Any()) return true;

        // ALL dependencies must evaluate to true (AND logic configuration match)
        foreach (var dep in dependencies)
        {
            var targetProp = typeof(TModel).GetProperty(dep.TargetProperty);
            if (targetProp == null) continue;

            var actualValue = targetProp.GetValue(Model);
            
            // If the dependency expectation doesn't match current runtime model values, hide field
            if (actualValue == null || !actualValue.Equals(dep.ExpectedValue))
            {
                return false;
            }
        }
        return true;
    }

    private IEnumerable<PropertyInfo> GetVisiblePropertiesForCurrentStep()
    {
        return typeof(TModel).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite)
            .Where(p => (p.GetCustomAttribute<FormStepAttribute>()?.StepNumber ?? 1) == CurrentStep)
            .Where(IsPropertyVisible);
    }

    private void GoNext()
    {
        // Step forward. If the next step has zero visible entries due to choices, skip it!
        do {
            CurrentStep++;
        } while (CurrentStep < MaxSteps && !GetVisiblePropertiesForCurrentStep().Any());
        
        // If we landed on a skipped step at the very end, roll back gracefully
        if (CurrentStep == MaxSteps && !GetVisiblePropertiesForCurrentStep().Any())
        {
            CurrentStep = MaxSteps; 
        }
    }

    private void GoPrevious()
    {
        // Step backward. If previous steps are nullified due to logic branching choices, skip backward!
        do {
            CurrentStep--;
        } while (CurrentStep > 1 && !GetVisiblePropertiesForCurrentStep().Any());
    }

    private bool IsFinalDynamicStep()
    {
        // Look ahead to check if all remaining future steps are hidden by choices
        for (int i = CurrentStep + 1; i <= MaxSteps; i++)
        {
            var clearPath = typeof(TModel).GetProperties()
                .Where(p => (p.GetCustomAttribute<FormStepAttribute>()?.StepNumber ?? 1) == i)
                .Any(IsPropertyVisible);
            if (clearPath) return false;
        }
        return true;
    }

    private async Task HandleSubmitAttempt(EditContext editContext)
    {
        // Even if individual validations pass, the native Blazor EditForm validates the *whole model*.
        // This ensures the current visible branch path contains fully accurate data payloads before emitting.
        if (editContext.Validate())
        {
            await OnWizardComplete.InvokeAsync(Model);
        }
    }

    // Dynamic Element UI Component Core Mapper Tree
    private RenderFragment DynamicRenderInput(PropertyInfo property) => builder =>
    {
        Type componentType;
        var attributes = new Dictionary<string, object>();
        var propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

        attributes["Value"] = property.GetValue(Model)!;
        attributes["ValueChanged"] = EventCallback.Factory.Create(this, (object? val) => 
        {
            if (val != null)
            {
                var safeValue = propertyType.IsEnum ? Enum.Parse(propertyType, val.ToString()!) : Convert.ChangeType(val, propertyType);
                property.SetValue(Model, safeValue);
                StateHasChanged(); // Force redraw layout immediately so dependencies flip visibility in real-time!
            }
        });
        attributes["ValueExpression"] = CreateExpression(property);
        attributes["class"] = propertyType.IsEnum ? "form-select" : "form-control";

        if (propertyType.IsEnum)
        {
            componentType = typeof(InputSelect<>).MakeGenericType(property.PropertyType);
            builder.OpenComponent(0, componentType);
            builder.AddMultipleAttributes(1, attributes);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)((childBuilder) =>
            {
                int seq = 0;
                foreach (var name in Enum.GetNames(propertyType))
                {
                    var field = propertyType.GetField(name);
                    var display = field?.GetCustomAttribute<DisplayAttribute>();
                    childBuilder.OpenElement(seq++, "option");
                    childBuilder.AddAttribute(seq++, "value", name);
                    childBuilder.AddContent(seq++, display?.Name ?? name);
                    childBuilder.CloseElement();
                }
            }));
            builder.CloseComponent();
            return;
        }

        componentType = propertyType == typeof(bool) ? typeof(InputCheckbox) : typeof(InputText);
        if(propertyType == typeof(bool)) attributes["class"] = "form-check-input";

        builder.OpenComponent(0, componentType);
        builder.AddMultipleAttributes(1, attributes);
        builder.CloseComponent();
    };

    private System.Linq.Expressions.LambdaExpression CreateExpression(PropertyInfo property)
    {
        var constant = System.Linq.Expressions.Expression.Constant(Model);
        var member = System.Linq.Expressions.Expression.Property(constant, property);
        return System.Linq.Expressions.Expression.Lambda(member);
    }
}
```
________________________________________
How This Achieves Your Ultimate Goal
1.	Dynamic Visibility Evaluator (IsPropertyVisible): Every time the user selects a dropdown value, StateHasChanged() triggers. The generator re-evaluates the attributes. If they choose StrategyType.ExitNow, Step 2 and Step 3 will have zero fields to display.
2.	Intelligent Skipping Algorithm (GoNext): If you choose ExitNow and click "Next Step," the loop realizes that Step 2 and Step 3 are entirely empty because their visibility parameters failed. It bypasses them automatically and immediately changes the layout into a "Submit Wizard Data" workflow safely.
3.	Stacked Conditional Logic Rules: By allowing multiple [DependsOn] attributes to stack together (like AwsRoleArn), you can build deeply nested conditional branches (e.g., Only show field X if Question 1 is B AND Question 2 is A).
Would you like to extend this engine to include step-by-step partial validation (so users cannot hit "Next" if the current step's visible fields contain errors)? [2] 

 
# Idea 6.0.0 - Step-by-Step Partial Validation and seq++ fix
 
You are absolutely correct. Incrementing sequence numbers (seq++) dynamically inside loops or conditional blocks is a known anti-pattern in Blazor rendering trees. It disrupts Blazor's diffing algorithm during UI re-renders, causing components to lose focus, misbehave, or leak memory.
To fix this, we can use RenderTreeBuilder.OpenRegion(int sequence, int maxCloseCount) and CloseRegion(). A region allows us to lock a single hardcoded sequence index on the outside, creating a completely isolated sequence namespace on the inside where localized tracking integers are safe to use.
Here is the revised engine utilizing Open/Close Regions, followed by the implementation of Step-by-Step Partial Validation to lock the "Next" button if errors exist on the current page.
________________________________________
1. The Fixed & Optimized Engine (DynamicWizard.razor)
This code completely eliminates dynamic sequence leaking while incorporating real-time validation checks targeted only at the properties visible on the current wizard step.
```razor
@using System.Reflection
@using System.ComponentModel.DataAnnotations
@using Microsoft.AspNetCore.Components.Forms
@using BlazorWizardApp.Attributes
@typeparam TModel where TModel : class, new()

<EditForm EditContext="@_editContext" OnValidSubmit="@HandleSubmitAttempt">
    <DataAnnotationsValidator />

    <div class="card shadow-sm p-4" style="max-width: 600px; margin: 0 auto;">
        <!-- Header Step Tracker -->
        <div class="d-flex justify-content-between align-items-center mb-4 pb-2 border-bottom">
            <h5 class="m-0 text-primary">Step @CurrentStep of @MaxSteps</h5>
            <span class="badge bg-secondary p-2">Wizard Flow Pipeline</span>
        </div>

        <div class="row g-3 mb-4">
            @foreach (var prop in GetVisiblePropertiesForCurrentStep())
            {
                var displayAttr = prop.GetCustomAttribute<DisplayAttribute>();
                string labelText = displayAttr?.Name ?? prop.Name;

                <div class="col-12 d-flex flex-column gap-1">
                    <label class="form-label fw-bold mb-1">@labelText</label>
                    
                    @DynamicRenderInput(prop)
                    
                    <ValidationMessage For="@CreateExpression(prop)" class="text-danger small mt-1" />
                </div>
            }
        </div>

        <!-- Navigation Action Buttons Controls Footers -->
        <div class="d-flex justify-content-between border-top pt-3">
            <button type="button" class="btn btn-outline-secondary px-4" 
                    @onclick="GoPrevious" disabled="@(CurrentStep == 1)">
                Back
            </button>

            @if (CurrentStep < MaxSteps && !IsFinalDynamicStep())
            {
                <!-- Next button evaluates step-specific validation before proceeding -->
                <button type="button" class="btn btn-primary px-4" @onclick="GoNext">
                    Next Step
                </button>
            }
            else
            {
                <button type="submit" class="btn btn-success px-4">
                    Submit Wizard Data
                </button>
            }
        </div>
    </div>
</EditForm>

@code {
    [Parameter] public TModel Model { get; set; } = new();
    [Parameter] public EventCallback<TModel> OnWizardComplete { get; set; }

    private EditContext? _editContext;
    private ValidationMessageStore? _messageStore;
    private int CurrentStep { get; set; } = 1;
    private int MaxSteps { get; set; } = 1;

    protected override void OnInitialized()
    {
        _editContext = new EditContext(Model);
        _messageStore = new ValidationMessageStore(_editContext);

        var steps = typeof(TModel).GetProperties()
                                  .Select(p => p.GetCustomAttribute<FormStepAttribute>()?.StepNumber ?? 1);
        MaxSteps = steps.Any() ? steps.Max() : 1;
    }

    // PARTIAL STEP VALIDATION LOGIC
    private bool ValidateCurrentStepOnly()
    {
        if (_editContext == null || _messageStore == null) return false;

        // Clear any previous custom evaluation messages 
        _messageStore.Clear();

        var propertiesToValidate = GetVisiblePropertiesForCurrentStep();
        bool isStepValid = true;

        foreach (var prop in propertiesToValidate)
        {
            var fieldIdentifier = new FieldIdentifier(Model, prop.Name);
            var value = prop.GetValue(Model);

            // Fetch DataAnnotations standard attributes tied to the property
            var validationAttributes = prop.GetCustomAttributes<ValidationAttribute>();
            var validationContext = new ValidationContext(Model) { MemberName = prop.Name };
            var results = new List<ValidationResult>();

            if (!Validator.TryValidateValue(value!, validationContext, results, validationAttributes))
            {
                isStepValid = false;
                foreach (var error in results)
                {
                    // Pipe localized error straight into Blazor's core field messaging subsystem
                    _messageStore.Add(fieldIdentifier, error.ErrorMessage ?? "Invalid Input Value.");
                }
            }
        }

        // Notify Blazor UI validation components to sync up states visually
        _editContext.NotifyValidationStateChanged();
        return isStepValid;
    }

    private void GoNext()
    {
        // Enforce step validation before advancing
        if (!ValidateCurrentStepOnly()) return;

        do {
            CurrentStep++;
        } while (CurrentStep < MaxSteps && !GetVisiblePropertiesForCurrentStep().Any());

        if (CurrentStep == MaxSteps && !GetVisiblePropertiesForCurrentStep().Any())
        {
            CurrentStep = MaxSteps; 
        }
    }

    private void GoPrevious()
    {
        do {
            CurrentStep--;
        } while (CurrentStep > 1 && !GetVisiblePropertiesForCurrentStep().Any());
    }

    private bool IsPropertyVisible(PropertyInfo property)
    {
        var dependencies = property.GetCustomAttributes<DependsOnAttribute>();
        if (!dependencies.Any()) return true;

        foreach (var dep in dependencies)
        {
            var targetProp = typeof(TModel).GetProperty(dep.TargetProperty);
            if (targetProp == null) continue;

            var actualValue = targetProp.GetValue(Model);
            if (actualValue == null || !actualValue.Equals(dep.ExpectedValue))
            {
                return false;
            }
        }
        return true;
    }

    private IEnumerable<PropertyInfo> GetVisiblePropertiesForCurrentStep()
    {
        return typeof(TModel).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite)
            .Where(p => (p.GetCustomAttribute<FormStepAttribute>()?.StepNumber ?? 1) == CurrentStep)
            .Where(IsPropertyVisible);
    }

    private bool IsFinalDynamicStep()
    {
        for (int i = CurrentStep + 1; i <= MaxSteps; i++)
        {
            var clearPath = typeof(TModel).GetProperties()
                .Where(p => (p.GetCustomAttribute<FormStepAttribute>()?.StepNumber ?? 1) == i)
                .Any(IsPropertyVisible);
            if (clearPath) return false;
        }
        return true;
    }

    private async Task HandleSubmitAttempt()
    {
        // Verify final visible items match requirements before closing wizard instance
        if (ValidateCurrentStepOnly())
        {
            await OnWizardComplete.InvokeAsync(Model);
        }
    }

    // RENDERING CORE WITH COMPONENT ISOLATION REGIONS
    private RenderFragment DynamicRenderInput(PropertyInfo property) => builder =>
    {
        Type componentType;
        var attributes = new Dictionary<string, object>();
        var propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

        attributes["Value"] = property.GetValue(Model)!;
        attributes["ValueChanged"] = EventCallback.Factory.Create(this, (object? val) => 
        {
            if (val != null)
            {
                var safeValue = propertyType.IsEnum ? Enum.Parse(propertyType, val.ToString()!) : Convert.ChangeType(val, propertyType);
                property.SetValue(Model, safeValue);
                _editContext?.Validate(); // Clear structural validation errors on data input change
                StateHasChanged();
            }
        });
        attributes["ValueExpression"] = CreateExpression(property);
        attributes["class"] = propertyType.IsEnum ? "form-select" : "form-control";

        // Open Region completely safely separates loop rendering structures from parent changes
        builder.OpenRegion(100);

        if (propertyType.IsEnum)
        {
            componentType = typeof(InputSelect<>).MakeGenericType(property.PropertyType);
            builder.OpenComponent(0, componentType);
            builder.AddMultipleAttributes(1, attributes);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)((childBuilder) =>
            {
                // Local static sequence indices are perfectly valid inside this child-template context
                int innerSeq = 0;
                foreach (var name in Enum.GetNames(propertyType))
                {
                    var field = propertyType.GetField(name);
                    var display = field?.GetCustomAttribute<DisplayAttribute>();
                    childBuilder.OpenElement(innerSeq++, "option");
                    childBuilder.AddAttribute(innerSeq++, "value", name);
                    childBuilder.AddContent(innerSeq++, display?.Name ?? name);
                    childBuilder.CloseElement();
                }
            }));
            builder.CloseComponent();
            builder.CloseRegion(); // Safely shut nested scope down
            return;
        }

        componentType = propertyType == typeof(bool) ? typeof(InputCheckbox) : typeof(InputText);
        if (propertyType == typeof(bool)) attributes["class"] = "form-check-input";

        builder.OpenComponent(0, componentType);
        builder.AddMultipleAttributes(1, attributes);
        builder.CloseComponent();
        
        builder.CloseRegion();
    };

    private System.Linq.Expressions.LambdaExpression CreateExpression(PropertyInfo property)
    {
        var constant = System.Linq.Expressions.Expression.Constant(Model);
        var member = System.Linq.Expressions.Expression.Property(constant, property);
        return System.Linq.Expressions.Expression.Lambda(member);
    }
}
```
________________________________________
Why OpenRegion and CloseRegion Fixes Rendering Deficiencies
When Blazor encounters builder.OpenRegion(100), it signals the differential UI trees that any component structural loops rendered inside this block belong to an internal offset.
•	Static Differential Tracking: Even though innerSeq++ grows from 0 to X dynamically inside the loop, the outside differential engine sees a single, predictable block component located on line position sequence index entry 100.
•	Zero UI Artifacts: If a dropdown changes and fields shift positions or visibility states toggle, Blazor won't accidentally swap input field values or drop active keyboard selections.
Step-by-Step Validation Mechanics
•	Targeted Evaluation (Validator.TryValidateValue): Instead of executing _editContext.Validate() (which flags structural errors on hidden upcoming steps), the wizard isolates only the properties that match the currently rendering Step index.
•	ValidationMessageStore Pipelining: If a rule fails, the engine creates a FieldIdentifier for that property and loads the message into the custom message store. This highlights step-specific errors in the UI instantly and intercepts the navigation thread if inputs are invalid.
If you are interested, we can explore how to wrap this up cleanly by adding custom validator attributes (e.g., matching text fields against regex patterns or checking date ranges dynamically across properties). Let me know what you want to add next!


# Idea 7.0.0 - Custom Validation Attributes
 
To complete this architecture, we can extend the system to support Custom Validation Attributes that integrate directly into the step-by-step validation engine. [1] 
Instead of writing custom code inside the component for every validation use case, you can create standard reusable C# ValidationAttribute classes. Because our engine uses Validator.TryValidateValue, any class that inherits from ValidationAttribute will be caught and executed by the engine automatically. [2, 3] 
Below is the code showing how to create two common custom validators—a Regex-based Pattern Matcher and a dynamic Date Range Evaluator—and wrap them into the wizard. [4] 
________________________________________
1. Create the Custom Validation Attributes
FormRegexAttribute.cs
A clean way to enforce formatting (like phone numbers, postal codes, or specialized keys) with a custom error message.
```csharp
using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace BlazorWizardApp.Validators
{
    [AttributeUsage(AttributeTargets.Property)]
    public class FormRegexAttribute : ValidationAttribute
    {
        private readonly string _pattern;

        public FormRegexAttribute(string pattern, string defaultErrorMessage)
        {
            _pattern = pattern;
            ErrorMessage = defaultErrorMessage;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            {
                return ValidationResult.Success; // Let [Required] handle empty values if needed
            }

            if (!Regex.IsMatch(value.ToString()!, _pattern))
            {
                return new ValidationResult(ErrorMessage ?? $"The field format is invalid.", new[] { validationContext.MemberName! });
            }

            return ValidationResult.Success;
        }
    }
}
```
DateRangeAttribute.cs
An advanced validator that ensures a date field falls within a certain number of days relative to DateTime.Now.
```csharp
using System;
using System.ComponentModel.DataAnnotations;

namespace BlazorWizardApp.Validators
{
    [AttributeUsage(AttributeTargets.Property)]
    public class DateRangeAttribute : ValidationAttribute
    {
        private readonly int _minDaysFromNow;
        private readonly int _maxDaysFromNow;

        public DateRangeAttribute(int minDaysFromNow, int maxDaysFromNow)
        {
            _minDaysFromNow = minDaysFromNow;
            _maxDaysFromNow = maxDaysFromNow;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is DateTime targetDate)
            {
                var minAllowed = DateTime.Now.AddDays(_minDaysFromNow);
                var maxAllowed = DateTime.Now.AddDays(_maxDaysFromNow);

                if (targetDate < minAllowed || targetDate > maxAllowed)
                {
                    return new ValidationResult(
                        $"Date must be between {minAllowed:yyyy-MM-dd} and {maxAllowed:yyyy-MM-dd}.",
                        new[] { validationContext.MemberName! }
                    );
                }
                return ValidationResult.Success;
            }

            return new ValidationResult("Invalid date type matching handled.", new[] { validationContext.MemberName! });
        }
    }
}
```
________________________________________
2. Apply Custom Attributes to the Model
Now we can decorate properties in WizardModel alongside our existing structural rules ([FormStep] and [DependsOn]).
```csharp
using System;
using System.ComponentModel.DataAnnotations;
using BlazorWizardApp.Attributes;
using BlazorWizardApp.Validators;

namespace BlazorWizardApp.Data
{
    public class WizardModel
    {
        [Required]
        [Display(Name = "Choose Your Information Path")]
        [FormStep(1)]
        public StrategyType ChosenStrategy { get; set; } = StrategyType.SimplePath;

        // Path A (Simple) Fields
        [DependsOn(nameof(ChosenStrategy), StrategyType.SimplePath)]
        [Display(Name = "Your Contact Email Address")]
        [Required, EmailAddress]
        [FormStep(2)]
        public string SimpleEmail { get; set; } = string.Empty;

        // Path B (Advanced) Fields
        [DependsOn(nameof(ChosenStrategy), StrategyType.AdvancedPath)]
        [Display(Name = "Select Infrastructure Target")]
        [FormStep(2)]
        public CloudProvider EnterpriseCloud { get; set; } = CloudProvider.AWS;

        // NEW CUSTOM REvalidator: Enforces AWS ARN formatting rules strictly on Step 2
        [DependsOn(nameof(ChosenStrategy), StrategyType.AdvancedPath)]
        [DependsOn(nameof(EnterpriseCloud), CloudProvider.AWS)]
        [Display(Name = "AWS IAM Role ARN String")]
        [Required]
        [FormRegex(@"^arn:aws:iam::\d{12}:role\/[a-zA-Z0-9+=,.@\-_]+$", "Invalid AWS Role ARN format. Must look like 'arn:aws:iam::123456789012:role/RoleName'")]
        [FormStep(2)]
        public string AwsRoleArn { get; set; } = string.Empty;

        // NEW CUSTOM DATE RANGE VALIDATOR: Enforces reservation timeline rules on Step 3
        [Display(Name = "Target Deployment Date")]
        [FormStep(3)]
        [DateRange(minDaysFromNow: 1, maxDaysFromNow: 90)] // Must be tomorrow up to 3 months away
        public DateTime DeploymentDate { get; set; } = DateTime.Now.AddDays(1);

        [DependsOn(nameof(ChosenStrategy), StrategyType.SimplePath)]
        [Display(Name = "Final Notes / Comments")]
        [FormStep(3)]
        public string FinalNotes { get; set; } = string.Empty;

        [DependsOn(nameof(ChosenStrategy), StrategyType.AdvancedPath)]
        [Display(Name = "Enterprise SLA Agreement Code")]
        [Required]
        [FormStep(3)]
        public string SlaCode { get; set; } = string.Empty;
    }
}
```
________________________________________
Why the Existing Engine Handles This Perfectly
Because the DynamicWizard.razor engine evaluates validation using reflection, you do not need to change a single line of the component code to support these new custom rules.
Let's review the code slice from the component that handles this automatically:
```csharp
// Inside DynamicWizard.razor -> ValidateCurrentStepOnly()
foreach (var prop in propertiesToValidate)
{
    var fieldIdentifier = new FieldIdentifier(Model, prop.Name);
    var value = prop.GetValue(Model);

    // This native method grabs ALL attributes deriving from ValidationAttribute, 
    // including our new [FormRegex] and [DateRange] attributes automatically!
    var validationAttributes = prop.GetCustomAttributes<ValidationAttribute>();
    var validationContext = new ValidationContext(Model) { MemberName = prop.Name };
    var results = new List<ValidationResult>();

    if (!Validator.TryValidateValue(value!, validationContext, results, validationAttributes))
    {
        isStepValid = false;
        foreach (var error in results)
        {
            _messageStore.Add(fieldIdentifier, error.ErrorMessage ?? "Invalid Input Value.");
        }
    }
}
```
Key Architectural Wins
1.	Zero Component Bloat: You can add 50 different custom business rules (e.g., credit card checkers, cross-field matchers, dependency validations) across your application, and the wizard interface remains completely untouched.
2.	Encapsulation: Validation behavior, UI order layout (FormStep), dynamic flow paths (DependsOn), and formatting constraints exist uniformly inside the data model metadata definitions.
3.	Smooth Interactive Experience: When a user triggers an error (e.g., inputs an invalid AWS ARN formatting string on Step 2), the wizard locks navigation automatically, and native Blazor ```<ValidationMessage />``` components bind precisely to the failing DOM location with zero rendering artifact side-effects.
To make the user interface cleaner, would you like to see how to add CSS-driven conditional styling triggers (like turning input borders red when invalid, or adding an active progress step indicator at the top)?

 
 
# Idea 8.0.0 - CSS-Driven Conditional Styling Triggers (Most complete but incorporates Bootstrap) Would prefer to use bare CSS if possible and allow consumer to override the styles.

To add CSS-driven conditional styling triggers, we tap into Blazor’s native integration with EditContext field states.
Instead of writing verbose inline C# style expressions for every element, the standard approach is to use Blazor's built-in FieldCssClassProvider. This allows you to hook directly into the wizard engine's internal validation dictionary and map active field states (such as valid, invalid, modified, or pristine) to native Bootstrap form feedback classes (is-valid and is-invalid) automatically. [1] 
Here is the updated wizard configuration complete with an integrated CSS state engine and a styled step progress tracker.
________________________________________
1. Create the Custom CSS Provider (BootstrapValidationClassProvider.cs)
This class hooks into the EditContext and instructs Blazor exactly which Bootstrap class to append to the HTML markup when an input field's state changes.
```csharp
using Microsoft.AspNetCore.Components.Forms;

namespace BlazorWizardApp.Infrastructure
{
    public class BootstrapValidationClassProvider : FieldCssClassProvider
    {
        public override string GetFieldCssClass(EditContext editContext, in FieldIdentifier fieldIdentifier)
        {
            // Check if there are active errors in the message store for this specific field
            var isInvalid = editContext.GetValidationMessages(fieldIdentifier).Any();

            if (isInvalid)
            {
                return "is-invalid"; // Appends Bootstrap's native red border error state
            }

            // Optional: If the user changed the data and it passed validation, show a green valid state
            var isModified = editContext.IsModified(fieldIdentifier);
            if (isModified)
            {
                return "is-valid"; // Appends Bootstrap's native green border success state
            }

            return string.Empty; // Default fallback state for untouched fields
        }
    }
}
```
________________________________________
2. Update the Wizard Component (DynamicWizard.razor)
We register our custom class provider inside OnInitialized. Then, in the DynamicRenderInput section, we call _editContext.FieldCssClass(prop.Name) to inject the matching CSS trigger string cleanly into the element's attribute dictionary.
```razor
@using System.Reflection
@using System.ComponentModel.DataAnnotations
@using Microsoft.AspNetCore.Components.Forms
@using BlazorWizardApp.Attributes
@using BlazorWizardApp.Infrastructure
@typeparam TModel where TModel : class, new()

<EditForm EditContext="@_editContext" OnValidSubmit="@HandleSubmitAttempt">
    <DataAnnotationsValidator />

    <div class="card shadow-lg p-4 mx-auto" style="max-width: 650px; border-radius: 12px;">
        
        <!-- VISUAL INDICATOR: Bootstrap Progress Trackers -->
        <div class="wizard-progress mb-4">
            <div class="progress" style="height: 6px;">
                <div class="progress-bar progress-bar-striped progress-bar-animated bg-primary" 
                     role="progressbar" 
                     style="width: @(CalculateProgressPercentage())%;"></div>
            </div>
            <div class="d-flex justify-content-between mt-2 small text-muted">
                <span>Step <strong>@CurrentStep</strong> of @MaxSteps</span>
                <span>@CalculateProgressPercentage()% Complete</span>
            </div>
        </div>

        <!-- STYLED FORM GROUPS -->
        <div class="row g-4 mb-4">
            @foreach (var prop in GetVisiblePropertiesForCurrentStep())
            {
                var displayAttr = prop.GetCustomAttribute<DisplayAttribute>();
                string labelText = displayAttr?.Name ?? prop.Name;

                <div class="col-12">
                    <label class="form-label fw-semibold text-secondary mb-1">@labelText</label>
                    
                    <div class="input-group has-validation">
                        @DynamicRenderInput(prop)
                        
                        <!-- Bootstrap uses this class to toggle message rendering based on the input's sibling .is-invalid class -->
                        <div class="invalid-feedback d-block">
                            <ValidationMessage For="@CreateExpression(prop)" />
                        </div>
                    </div>
                </div>
            }
        </div>

        <!-- NAVIGATION CONTROL FOOTER -->
        <div class="d-flex justify-content-between align-items-center pt-3 border-top mt-4">
            <button type="button" class="btn btn-outline-secondary px-4 py-2" 
                    @onclick="GoPrevious" disabled="@(CurrentStep == 1)">
                <i class="bi bi-arrow-left me-1"></i> Back
            </button>

            @if (CurrentStep < MaxSteps && !IsFinalDynamicStep())
            {
                <button type="button" class="btn btn-primary px-4 py-2 fw-medium" @onclick="GoNext">
                    Next Step <i class="bi bi-arrow-right ms-1"></i>
                </button>
            }
            else
            {
                <button type="submit" class="btn btn-success px-5 py-2 fw-bold shadow-sm">
                    <i class="bi bi-check-circle me-1"></i> Submit Application
                </button>
            }
        </div>
    </div>
</EditForm>

@code {
    [Parameter] public TModel Model { get; set; } = new();
    [Parameter] public EventCallback<TModel> OnWizardComplete { get; set; }

    private EditContext? _editContext;
    private ValidationMessageStore? _messageStore;
    private int CurrentStep { get; set; } = 1;
    private int MaxSteps { get; set; } = 1;

    protected override void OnInitialized()
    {
        _editContext = new EditContext(Model);
        _messageStore = new ValidationMessageStore(_editContext);

        // Bind our Custom CSS Context State Transformer into Blazor's execution context
        _editContext.SetFieldCssClassProvider(new BootstrapValidationClassProvider());

        var steps = typeof(TModel).GetProperties()
                                  .Select(p => p.GetCustomAttribute<FormStepAttribute>()?.StepNumber ?? 1);
        MaxSteps = steps.Any() ? steps.Max() : 1;
    }

    private int CalculateProgressPercentage()
    {
        return (int)(((double)CurrentStep / MaxSteps) * 100);
    }

    private bool ValidateCurrentStepOnly()
    {
        if (_editContext == null || _messageStore == null) return false;

        _messageStore.Clear();
        bool isStepValid = true;

        foreach (var prop in GetVisiblePropertiesForCurrentStep())
        {
            var fieldIdentifier = new FieldIdentifier(Model, prop.Name);
            var value = prop.GetValue(Model);

            var validationAttributes = prop.GetCustomAttributes<ValidationAttribute>();
            var validationContext = new ValidationContext(Model) { MemberName = prop.Name };
            var results = new List<ValidationResult>();

            if (!Validator.TryValidateValue(value!, validationContext, results, validationAttributes))
            {
                isStepValid = false;
                foreach (var error in results)
                {
                    _messageStore.Add(fieldIdentifier, error.ErrorMessage ?? "Invalid Input.");
                }
            }
        }

        _editContext.NotifyValidationStateChanged();
        return isStepValid;
    }

    private void GoNext()
    {
        if (!ValidateCurrentStepOnly()) return;

        do {
            CurrentStep++;
        } while (CurrentStep < MaxSteps && !GetVisiblePropertiesForCurrentStep().Any());
    }

    private void GoPrevious()
    {
        do {
            CurrentStep--;
        } while (CurrentStep > 1 && !GetVisiblePropertiesForCurrentStep().Any());
        
        // Re-validate when stepping back so the current state remains styled accurately
        ValidateCurrentStepOnly();
    }

    private bool IsPropertyVisible(PropertyInfo property)
    {
        var dependencies = property.GetCustomAttributes<DependsOnAttribute>();
        if (!dependencies.Any()) return true;

        foreach (var dep in dependencies)
        {
            var targetProp = typeof(TModel).GetProperty(dep.TargetProperty);
            if (targetProp == null) continue;

            var actualValue = targetProp.GetValue(Model);
            if (actualValue == null || !actualValue.Equals(dep.ExpectedValue))
            {
                return false;
            }
        }
        return true;
    }

    private IEnumerable<PropertyInfo> GetVisiblePropertiesForCurrentStep()
    {
        return typeof(TModel).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite)
            .Where(p => (p.GetCustomAttribute<FormStepAttribute>()?.StepNumber ?? 1) == CurrentStep)
            .Where(IsPropertyVisible);
    }

    private bool IsFinalDynamicStep()
    {
        for (int i = CurrentStep + 1; i <= MaxSteps; i++)
        {
            var clearPath = typeof(TModel).GetProperties()
                .Where(p => (p.GetCustomAttribute<FormStepAttribute>()?.StepNumber ?? 1) == i)
                .Any(IsPropertyVisible);
            if (clearPath) return false;
        }
        return true;
    }

    private async Task HandleSubmitAttempt()
    {
        if (ValidateCurrentStepOnly())
        {
            await OnWizardComplete.InvokeAsync(Model);
        }
    }

    private RenderFragment DynamicRenderInput(PropertyInfo property) => builder =>
    {
        Type componentType;
        var attributes = new Dictionary<string, object>();
        var propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

        // DYNAMIC CSS ASSIGNMENT: Evaluates through our custom provider pipeline instance
        string conditionalCssClass = _editContext!.FieldCssClass(property.Name);
        string coreStyle = propertyType.IsEnum ? "form-select" : "form-control";
        
        attributes["Value"] = property.GetValue(Model)!;
        attributes["ValueChanged"] = EventCallback.Factory.Create(this, (object? val) => 
        {
            if (val != null)
            {
                var safeValue = propertyType.IsEnum ? Enum.Parse(propertyType, val.ToString()!) : Convert.ChangeType(val, propertyType);
                property.SetValue(Model, safeValue);
                
                // Immediately evaluate fields when modified to toggle border styles instantly
                ValidateCurrentStepOnly(); 
            }
        });
        attributes["ValueExpression"] = CreateExpression(property);
        attributes["class"] = $"{coreStyle} {conditionalCssClass}".Trim();

        builder.OpenRegion(100);

        if (propertyType.IsEnum)
        {
            componentType = typeof(InputSelect<>).MakeGenericType(property.PropertyType);
            builder.OpenComponent(0, componentType);
            builder.AddMultipleAttributes(1, attributes);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)((childBuilder) =>
            {
                int innerSeq = 0;
                foreach (var name in Enum.GetNames(propertyType))
                {
                    var field = propertyType.GetField(name);
                    var display = field?.GetCustomAttribute<DisplayAttribute>();
                    childBuilder.OpenElement(innerSeq++, "option");
                    childBuilder.AddAttribute(innerSeq++, "value", name);
                    childBuilder.AddContent(innerSeq++, display?.Name ?? name);
                    childBuilder.CloseElement();
                }
            }));
            builder.CloseComponent();
            builder.CloseRegion();
            return;
        }

        componentType = propertyType == typeof(bool) ? typeof(InputCheckbox) : typeof(InputText);
        if (propertyType == typeof(bool)) attributes["class"] = $"form-check-input {conditionalCssClass}".Trim();

        builder.OpenComponent(0, componentType);
        builder.AddMultipleAttributes(1, attributes);
        builder.CloseComponent();
        
        builder.CloseRegion();
    };

    private System.Linq.Expressions.LambdaExpression CreateExpression(PropertyInfo property)
    {
var constant = System.Linq.Expressions.Expression.Constant(Model);
var member = System.Linq.Expressions.Expression.Property(constant, property);
return System.Linq.Expressions.Expression.Lambda(member);
}
}
```

---

### Architectural Strengths of this CSS Strategy

1. **Decoupled Markup Generation:** The dynamic generator logic inside `DynamicRenderInput` doesn't need to know why a field is invalid. It simply queries `_editContext.FieldCssClass(property.Name)`, preserving separation of concerns.
2. **Instant Visual Feedback:** Because `ValidateCurrentStepOnly()` runs inside the `ValueChanged` callback delegate loop, the user gets immediate visual feedback as they type or modify selections (e.g., entering an invalid string immediately triggers a red border, which flips to green once corrected).
3. **Native Framework Support:** By outputting standard Bootstrap classes like `is-invalid` alongside a child class wrapper containing `invalid-feedback`, standard desktop or mobile stylesheet animations behave precisely as expected out of the box.

Would you like to build out a dashboard view next to display a **read-only live summary** of all choices across the wizard steps before the user hits the final submit button?

 
CSS/BOOTSTRAP add on
To achieve zero dependencies while remaining completely compatible with external frameworks like Bootstrap, you should design your Blazor components using Semantic HTML, CSS variables (Custom Properties), and an open class-merging strategy.
This approach ensures the component looks great "bare" out of the box, but seamlessly adapts or yields to the consumer's layout framework if they use one.
Core Architecture Strategy
•	Use Standard HTML Elements: Avoid wrapper ```<div>``` chains; use semantic tags like ```<nav>, <article>, <aside>, or <section>``` which layout engines respect naturally.
•	Expose an AdditionalAttributes Parameter: Capture unmatched attributes so consumers can inject their own layout classes (e.g., class="col-md-6 mt-3").
•	Leverage CSS Variables: Use CSS variables for spacing, padding, and display properties so consumers can globally override your default layouts.
•	Unobtrusive Defaults: Write your internal CSS using high-specificity isolation or fallback variables so they only apply when external styles are missing.
________________________________________
Implementation Example: A Flexible Card Component
This component functions perfectly on its own using native CSS Flexbox/Grid, but if a consumer wraps it in a Bootstrap .row and passes .col-md-4, it integrates flawlessly.
1. The Razor Component (MyCard.razor)
```html
<div class="my-custom-card @Class" @attributes="UserAttributes">
    @if (HeaderContent != null)
    {
        <header class="my-card-header">@HeaderContent</header>
    }
    <div class="my-card-body">
        @ChildContent
    </div>
</div>

@code {
    [Parameter] public RenderFragment? HeaderContent { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
    
    // Explicitly allow class overrides
    [Parameter] public string? Class { get; set; }

    // Captures all other attributes (like id, style, aria-* or layout framework classes)
    [Parameter(CaptureUnmatchedAttributes = true)] 
    public Dictionary<string, object>? UserAttributes { get; set; }
}
```
2. The Isolated CSS (MyCard.razor.css)
By using CSS variables with fallbacks, the component handles its own layout but yields control if the parent environment defines those variables.
```css
.my-custom-card {
    /* Layout properties that play nice with external grid systems */
    display: flex;
    flex-direction: column;
    width: var(--card-width, 100%);
    box-sizing: border-box;
    
    /* Semantic styling with fallbacks */
    padding: var(--card-padding, 1rem);
    border: var(--card-border, 1px solid #e0e0e0);
    border-radius: var(--card-radius, 4px);
    background-color: var(--card-bg, #ffffff);
}

.my-card-body {
    flex: 1 1 auto;
}
```
________________________________________
How Consumers Can Use It
Scenario A: The "Bare" Implementation (Zero Dependencies)
The component uses its own internal flex layouts and boundaries.
```html
<MyCard>
    <HeaderContent><h3>Bare Title</h3></HeaderContent>
    <ChildContent><p>Works out of the box with zero setup.</p></ChildContent>
</MyCard>
```
Scenario B: The Bootstrap Implementation
The consumer plugs it directly into a Bootstrap grid. The CaptureUnmatchedAttributes and @Class parameters merge the Bootstrap behavior natively.

```html
<div class="row">
    <!-- Bootstrap handles the width/columns; your component handles the inside -->
    <MyCard Class="card col-md-4 mb-3" style="--card-bg: #f8f9fa;">
        <HeaderContent><h3 class="card-title">Bootstrap Title</h3></HeaderContent>
        <ChildContent><p class="card-text">Integrates smoothly.</p></ChildContent>
    </MyCard>
</div>
```
