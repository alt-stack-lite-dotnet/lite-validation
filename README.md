# Lite.Validation

Библиотека валидации для .NET, заточенная под производительность и минимальное давление на GC. Fluent API, опциональный source generator, интеграции с ASP.NET Core (MVC, FastEndpoints) и DI.

**NuGet:** [Lite.Validation](https://www.nuget.org/packages/Lite.Validation/)  
**Бенчмарки (статья с цифрами):** [BENCHMARKS.md](BENCHMARKS.md)

---

## Зачем это нужно

Валидация в веб-API вызывается на каждый запрос. В высоконагруженных сервисах это hot path: лишние наносекунды и аллокации складываются в миллисекунды и в постоянную нагрузку на сборщик мусора. Классические решения вроде FluentValidation или DataAnnotations удобны, но на каждый вызов тянут за собой рефлексию, скомпилированные делегаты, аллокации под результат и коллекции ошибок — в десятки раз больше времени и памяти, чем минимально необходимо.

Lite.Validation решает эту задачу иначе:

- **Source generator** генерирует код валидации на этапе компиляции. Никакой рефлексии и `Expression.Compile` в рантайме — только прямой код, который JIT хорошо инлайнит.
- **Нулевые аллокации на успешной валидации**: результат — структура, список ошибок создаётся только при наличии ошибок.
- **Тот же привычный fluent-подход** — правила описываются в коде через `RuleFor`, цепочки правил, условия, вложенные валидаторы. Можно начать с runtime-варианта (`LiteValidator` + билдер) и позже перейти на source-generated без смены API.

Итог: в бенчмарках при тех же правилах мы обходим FluentValidation по времени в десятки раз и многократно снижаем аллокации и число Gen0-сборок на 10 000 запросов. Подробные цифры, таблицы и комментарии — в [BENCHMARKS.md](BENCHMARKS.md).

---

## Пакеты

| Пакет | Описание |
|-------|----------|
| [**Lite.Validation**](https://www.nuget.org/packages/Lite.Validation/) | Ядро: `IValidator<T>`, `FluentValidator<T>`, `LiteValidator<T>`, встроенные правила. |
| **Lite.Validation.SourceGenerator** | Roslyn source generator: генерация `Validate()`/`ValidateAsync()` из `FluentValidator<T>` при компиляции. |
| **Lite.Validation.Rules.Inline** | Дополнительные inline-правила (подключается ядром). |
| **Lite.Validation.Integration.DependencyInjection** | `AddLiteValidatorsFromAssembly()` и регистрация в `IServiceCollection`. |
| **Lite.Validation.Integration.AspNetCore.Mvc** | Интеграция с ASP.NET Core MVC. |
| **Lite.Validation.Integration.AspNetCore.FastEndpoints** | Интеграция с [FastEndpoints](https://fast-endpoints.com/). |

---

## Быстрый старт

### Вариант с ручной конфигурацией (LiteValidator)

Подходит, когда валидатор создаётся вручную или через DI с передачей билдера. Правила задаются в конструкторе.

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

### Вариант с source generator (FluentValidator)

Подключи пакет **Lite.Validation.SourceGenerator**. Правила описываются в статическом `Configure`; генератор создаёт реализацию валидатора в compile time — без рефлексии и лишних аллокаций.

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

### Регистрация в DI

```csharp
services.AddLiteValidatorsFromAssemblyOf<OrderFluentValidator>(ServiceLifetime.Singleton);
```

---

## Сборка и тесты

```bash
dotnet build Lite.Validation.sln
dotnet test Lite.Validation.sln --no-build
```

---

## Бенчмарки

Подробная статья с замерами против FluentValidation и DataAnnotations, разбором по одному запросу и по 10 000 запросов (время, аллокации, оценка Gen0): **[BENCHMARKS.md](BENCHMARKS.md)**.

Запуск бенчмарков локально:

```bash
dotnet run -c Release --project benchmarks/Lite.Validation.Benchmarks -- --filter "*SimpleValidation*"
dotnet run -c Release --project benchmarks/Lite.Validation.Benchmarks -- --filter "*HighVolume*"
```

Отчёты (Markdown/HTML) сохраняются в `BenchmarkDotNet.Artifacts/results/`.

---

## Разработка: окружение и хуки

Установка (mise, Python venv, pre-commit, dotnet tools):

- **Windows (PowerShell):** `.\install.ps1`
- **Linux/macOS:** `./install.sh` (при необходимости: `chmod +x install.sh`)

При коммите запускаются форматтер (CSharpier) и сборка; при пуше — сборка и тесты.

Проверка хуков:

```bash
ls .git/hooks/pre-commit .git/hooks/pre-push
```

Ручной прогон:

```bash
pre-commit run --all-files
pre-commit run --hook-stage push --all-files
```

Если хуки не срабатывают: из корня выполни `pre-commit install` и `pre-commit install --hook-type pre-push`.

В `.vscode/` — рекомендуемые расширения и настройки (CSharpier, format on save).
