# Память по структуре validators

Создано при переносе проектов валидации в папку `validators/`. После переноса репо в другую папку можно опереться на этот файл.

## Структура

```
validators/
  Lite.Validation.sln   ← решение (собирать именно его для validation/benchmarks)
  sources/              ← продуктивные проекты
  test/                 ← тесты
  benchmarks/           ← бенчмарки
  playground/           ← эксперименты (пока пусто)
```

## Что куда перенесено

- **sources/**  
  Lite.Validation.Rules.Inline, Lite.Validation, Lite.Validation.SourceGenerator, Lite.Validation.DependencyInjection, Lite.Validation.Integration.AspNetCore.Mvc, Lite.Validation.Integration.AspNetCore.FastEndpoints.

- **test/**  
  Lite.Validation.Tests, Lite.Validation.SourceGenerator.Test, Lite.Validation.Integration.AspNetCore.Test.

- **benchmarks/**  
  Lite.Benchmarks, Lite.Benchmarks.AspNetCore, Lite.Benchmarks.AspNetCore.App.

## Ссылки между проектами

- Проекты в **test/** и **benchmarks/** ссылаются на **sources/** через `..\..\sources\ProjectName\ProjectName.csproj` (два уровня вверх от папки проекта, затем `sources\...`).
- Проекты внутри **sources/** ссылаются друг на друга через `..\ProjectName\` (один уровень вверх).

## Важные правки (уже внесены)

1. **Lite.Benchmarks.AspNetCore**  
   - Удалён пакет `BenchmarkDotNet.Running` (нет такого пакета, всё в BenchmarkDotNet).  
   - В .csproj задан `<StartupObject>Lite.Benchmarks.AspNetCore.Program</StartupObject>`, чтобы не было двух entry point (свой Program и Program из App).  
   - В `WebApplicationFactory<>` используется `AppEntryPoint`, а не `Program` (статический тип нельзя использовать как type argument).

2. **Lite.Benchmarks.AspNetCore.App**  
   - Добавлен класс `AppEntryPoint` для `WebApplicationFactory`.  
   - В `AddLiteValidatorsFromAssemblyOf<>` используется `OrderFluentValidator`, не `Program`.

3. **Корневой Lite.sln**  
   В нём по-прежнему старые пути к validation/benchmark проектам (они теперь в validators/). Для валидации и бенчмарков нужно открывать и собирать **validators/Lite.Validation.sln**.

## Сборка

```bash
dotnet build validators/Lite.Validation.sln
```

Ожидается: Build succeeded, 0 Error(s).
