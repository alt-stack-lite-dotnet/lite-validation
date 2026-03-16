# Публикация пакетов на NuGet.org

## Что нужно один раз

1. **API-ключ NuGet**
   - Зайди на [nuget.org → API Keys](https://www.nuget.org/account/apikeys).
   - **Create** → имя (например `GitHub`), **Push** (push new packages and versions). Скопируй ключ.

2. **Секрет в GitHub**
   - Репозиторий → **Settings** → **Secrets and variables** → **Actions**.
   - **New repository secret**: имя **`NUGET_API_KEY`**, значение — ключ из шага 1.

После этого пакеты будут выкладываться автоматически при пуше тега.

## Как выложить новую версию

Выкладка на NuGet запускается **только при пуше тега с ветки `release`**: если тег указывает на коммит не из `release`, job pack завершится с ошибкой.

1. Обнови версию в **`Directory.Build.props`** (например `0.2.0`).
2. Закоммить и запушь изменения в **ветку `release`** (через merge из своей ветки или напрямую).
3. На **`release`** создай тег и запушь его:

   ```bash
   git checkout release
   git pull origin release
   git tag v0.2.0
   git push origin v0.2.0
   ```

4. В **Actions** запустится workflow: сборка → тесты → проверка, что тег на `release` → pack → push на nuget.org.

Артефакт с `.nupkg` сохраняется в run; на nuget.org пакеты появятся только при заданном `NUGET_API_KEY`. Имя релиз-ветки задаётся в `ci.yml` переменной `RELEASE_BRANCH` (по умолчанию `release`).
