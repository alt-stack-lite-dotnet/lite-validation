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
| **Lite.Validation.Integration.DependencyInjection** | `AddLiteValidatorsFromAssembly()` и регистрация в `IServiceCollection`. |
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

## Бенчмарки

Сравнение с **FluentValidation** и **DataAnnotations** при одинаковых правилах (Name, Email, Age). Запуск:

```bash
dotnet run --project benchmarks/Lite.Validation.Benchmarks/Lite.Validation.Benchmarks.csproj -c Release -- --filter "*SimpleValidation*"
```

| Вариант | Valid (время) | Invalid (время) | Аллокации (Valid) |
|--------|----------------|-----------------|-------------------|
| FluentValidation (baseline) | 1.00× | 1.00× | baseline |
| **Lite.Validation (Runtime)** | см. вывод | см. вывод | обычно меньше |
| **Lite.Validation (Source-generated)** | быстрее | быстрее | минимум |
| DataAnnotations | см. вывод | см. вывод | см. вывод |

Source-generated вариант без рефлексии и `Expression.Compile` — ожидаемо быстрее и с меньшими аллокациями. Бенчмарки ASP.NET Core (MVC endpoint): проект `Lite.Validation.Benchmarks.AspNetCore`, класс `AspNetCoreValidationBenchmark`.

---

## Разработка: зависимости и хуки

Установка окружения (mise + Python venv + pre-commit + dotnet tools):

- **Windows (PowerShell):** `.\install.ps1`
- **Linux/macOS (Bash):** `./install.sh` (при необходимости: `chmod +x install.sh`)

После установки при **коммите** запускаются форматтер (CSharpier) и сборка; при **пуше** — сборка и тесты.

**Проверить, что хуки стоят:**

```bash
ls .git/hooks/pre-commit .git/hooks/pre-push
```

**Запустить проверки вручную (без коммита):**

```bash
pre-commit run --all-files          # все хуки pre-commit
pre-commit run --hook-stage push --all-files   # хуки pre-push
```

Если хуки не срабатывают при `git commit` / `git push`, переустанови: из корня репо выполни `pre-commit install` и `pre-commit install --hook-type pre-push`. На Windows убедись, что `git commit` запускается из терминала, где доступны `sh` и `python` (например, Git Bash или PowerShell после установки через install.ps1).

**VS Code / Cursor:** в `.vscode/` лежат рекомендуемые расширения (`extensions.json`) и форматирование при сохранении (`settings.json`: CSharpier как форматтер для C#). При открытии репо IDE предложит установить расширения из списка.

---
