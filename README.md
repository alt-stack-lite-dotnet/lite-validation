# Lite.Validation

**Лёгкая валидация для .NET без магии и без лишних аллокаций.** Fluent API, опциональный source generator для нулевого оверхеда, интеграции с ASP.NET Core (MVC, FastEndpoints) и DI.

Цель — ZO/ZH/SG-first либа для валидации: zero overhead, zero heap там, где возможно, и опора на source generator.

---

## Зачем это

- **Простой fluent API** — `RuleFor(x => x.Name).NotEmpty().WithDetails("...")`, синхронные и асинхронные правила.
- **Source generator** — можно описать правила через `FluentValidator<T>` + `partial class`; генератор выдаёт готовый `Validate()`/`ValidateAsync()` без рефлексии и `Expression.Compile`, только инлайнящийся код.
- **Мало зависимостей** — ядро на `netstandard2.1`, генератор — отдельный пакет, подключаешь только то, что нужно.
- **Интеграция с ASP.NET Core** — автоматическая валидация в MVC и FastEndpoints, регистрация валидаторов через DI.

Если тебе надоели тяжёлые или неудобные валидаторы и хочется чего-то своего — добро пожаловать.

---

## Пакеты

| Пакет | Описание |
|-------|----------|
| **Lite.Validation** | Ядро: `IValidator<T>`, `FluentValidator<T>`, `LiteValidator<T>`, встроенные правила. |
| **Lite.Validation.SourceGenerator** | Roslyn source generator: генерация валидаторов из `FluentValidator<T>` на этапе компиляции. |
| **Lite.Validation.Rules.Inline** | Дополнительные inline-правила (подключается ядром). |
| **Lite.Validation.DependencyInjection** | `AddLiteValidatorsFromAssembly()` и регистрация в `IServiceCollection`. |
| **Lite.Validation.Integration.AspNetCore.Mvc** | Поддержка ASP.NET Core MVC (модель и фильтры). |
| **Lite.Validation.Integration.AspNetCore.FastEndpoints** | Поддержка [FastEndpoints](https://fast-endpoints.com/). |

---

## Быстрый старт

### Ручная конфигурация (LiteValidator)

```csharp
public partial class CreateOrderValidator : LiteValidator<CreateOrderRequest>
{
    public CreateOrderValidator(ValidationBuilder<CreateOrderRequest> b) : base(b)
    {
        b.RuleFor(x => x.ProductName)
            .NotNull().WithDetails("Product name is required")
            .NotEmpty().WithDetails("Product name must not be empty");
        b.RuleFor(x => x.Quantity)
            .GreaterThan(0).WithDetails("Quantity must be positive");
    }
}
```

### Source-generated (FluentValidator + генератор)

Подключи пакет **Lite.Validation.SourceGenerator**, затем:

```csharp
public partial class OrderFluentValidator : FluentValidator<CreateOrderRequest>
{
    static void Configure(ValidationBuilder<CreateOrderRequest> b)
    {
        b.RuleFor(x => x.ProductName).NotNull().NotEmpty();
        b.RuleFor(x => x.Quantity).GreaterThan(0);
    }
}
```

Генератор создаёт реализацию `Validate()`/`ValidateAsync()` на этапе компиляции — без рефлексии и лишних аллокаций.

### DI

```csharp
services.AddLiteValidatorsFromAssemblyOf<OrderFluentValidator>(ServiceLifetime.Singleton);
```

---

## Сборка и тесты

Решение собирает все проекты (sources, test, benchmarks). Для валидации и бенчмарков используй этот sln:

```bash
dotnet build Lite.Validation.sln
dotnet test Lite.Validation.sln --no-build
```

---

## CI и публикация

- **Сборка и тесты**: на каждый push в `main`/`master` и на PR запускаются `dotnet build` и `dotnet test`.
- **Пакеты**: при пуше тега (например `v0.1.0`) собираются NuGet-пакеты и загружаются артефактом. Если в настройках репо задан секрет `NUGET_API_KEY`, пакеты автоматически пушатся на nuget.org.

Перед первым пушем замени в `Directory.Build.props` и в `nuget/Lite.Validation.nuspec` URL репозитория на свой.

---

## Лицензия

См. репозиторий. Мы делаем это в свободное от разработки игр время и рады, если кому-то пригодится.
