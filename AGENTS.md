# AGENTS.md — Nitra

Язык-воркбенч [rsdn/nitra](https://github.com/rsdn/nitra) + внутренний форк. Ядро проекта (компилятор, грамматика, рантайм) написано на **Nemerle** (`.n`, `.nproj`), IDE-плагины и тулзы — на C# (`.cs`, `.csproj`, net461).

## Удалённые git

- `github` — апстрим `rsdn/nitra`; `kl` — внутренний TFS (`Cons-TDL`). Активная работа идёт в форке `kl`, текущая ветка `test`. Коммиты к багам TFS оформляются как `Fix Bug <id>: <описание>`. Не пушьте в `github`.

## Сборка — главное

- **`dotnet build` не работает.** Только MSBuild из VS + Nemerle-тулчейн. `dotnet` CLI не понимает `.nproj`, `Directory.Build.props` привязан к `Microsoft.Net.Compilers` из `packages\`.
- Для сборки `.nproj` (все не-boot проекты) нужен Nemerle: свойство `$(Nemerle)` в `.nproj` берётся из env `NemerleBinPathRoot\Net-4.0` (по умолчанию `%ProgramFiles%\Nemerle`). Если env не задан и Nemerle не установлен — `.nproj` не соберутся вообще.
- `Boot1\Net-4.0\` — закоммиченный готовый компилятор (вершина в `Boot1\version.xml`), он же источник `HintPath`-ссылок. Цепочка бутстрапа (скрипты в корне, требуют VS2017 из реестра `...\SxS\VS7\15.0`):
  1. `BuildBoot.cmd` — сборка stage `Boot2` из `Boot2\*.nproj` toolset'ом из `Boot1` (через `Common\BootTasks.proj`, логи в `Boot2\BootTasks.binlog`).
  2. `ShiftBoot.cmd` — промоция: `Boot2` → `Boot1`, инкремент версии, копирование свежих исходников из `Nitra\` в `Boot2\`, пересборка. Это меняет закоммиченные бинарники `Boot1` — только осознанно.
  3. `RebuildBoot.cmd` — то же, что BuildBoot, но `/t:Rebuild /tv:4.0`.
- **VSIX (VS2022) собирать через `ExternalTools\build-with-latest-msbuild.cmd <sln/csproj>`** — он через `vswhere` поднимает окружение самого свежего VS; из-под окружения VS2017 не проходит валидация vsix.

## Stage1/Stage2 и метаданные

Ядро собирается в два прохода (`Stage1` → `Stage2`, выход в `obj\<Config>\Stage2\`), компилятор собирает сам себя. Парсеры грамматик генерируются Nemerle-макросами из `Nitra.Compiler` на этапе компиляции (`MacroReference` в `Nitra.MSBuild.Sdk\Sdk.targets`), артефакт — dll + `.nitrametadata2`.

В `Nitra\Nitra.Runtime\` и `Nitra\DotNetLang\` лежат **закоммиченные** `*.Stage2.dll.nitrametadata2` — на них сидит проход Stage1. После правки `Nitra.Runtime`/`DotNetLang` нужно прогнать `UpdateStage1Metadata.cmd` (копирует свежие Stage2-метаданные из `obj\Debug\Stage2\`), иначе последующие сборки Stage1 будут использовать устаревшие метаданные.

## Решения (какое для чего)

- `Nitra.sln` — полное (ядро + грамматики + VS-плагины + тулзы).
- `NitraCore.sln` / `Nitra.Compiler.sln` — только ядро: `Nitra.Runtime`, `Nitra.Compiler`, `Nitra.Grammar`, `DotNetLang`, `DotNet.BackEnd.CCI` (+`System.Collections.Immutable.Light`).
- `Nitra-Stagt1.sln` (sic, опечатка в имени) — stage1 + плагины VS2017.
- `NitraTools.sln` — грамматики, LanguageCompiler, TestsLauncher, ClientServer.
- `Installer.sln` — WiX-установщик (`Installer\`).

## Тесты

NUnit/xunit здесь не основное. Основные тесты — golden-file через `Nitra.TestsLauncher` (Nemerle):

```
Nitra.TestsLauncher.exe <путь к .nsln> <Debug|Release>
```

Наборы перечислены в `Tests\Visualizer\*.nsln` (`All.nsln` — все); каждая папка вида `<Grammar>~<Rule>` — входной файл + gold. При падении печатается diff gold vs pretty-print и exit code -1. Консольные тесты должны запускаться с большим стеком — лаунчер сам поднимает поток со стеком 20 МБ*8, не оборачивайте логику в основные потоки без этого.

- `Nitra.TestsLauncher.Old`, `Nitra.Visualizer.Old`, `Nitra.TestsConverter` — легаси, не трогать вслепую.
- `Tests\CSharp.Grammar.Test` — утилита с зашитыми путями (`c:\P\...`), вне CI не запускается.
- `Tests\!Positive`, `Tests\!Negative`, `Tests\!Disabled` — пустые заглушки в этой ветке.

## Архитектура: куда смотреть

- `Nitra\` — ядро (Nemerle): `Nitra.Runtime` (AST/рантайм), `Nitra.Compiler` (макросы, генерация парсеров), `Nitra.Grammar` (`.nf`-грамматики), `DotNetLang` (модель .NET-языков), `ClientServer\` (client-server режим IDE).
- `Grammars\` — грамматики и примеры: `CSharp\CSharp.Grammar` (расширяемый C#), `Json`, `Samples\Calculator` (минимальный пример — отправная точка для экспериментов), `Cpp.Grammar`, `SimpleSharp.Compiler`.
- `Ide\` — плагины VS (актуальные правки внутреннего форка, «TDL»-баги, в основном здесь). `Ide\NitraCommonVSIX\` устроен так: `src\` — общий код, `Vsix2017\`/`Vsix2022\` — отдельные головы под каждую VS; `NitraCommonIde` и `Nitra.VisualStudio.Plugin`/`DotNetLang.VisualStudio.Plugin` — переиспользуемые плагины.
- `Nitra.LanguageCompiler\Templates\XXLanguageFullNameXXVsPackage\` — шаблон генерируемого VS-пакета для нового языка (classifiers и т.п. генерируются LanguageCompiler'ом).
- `Boot1\`, `Boot2\`, `bin\`, `packages\`, `obj\` — артефакты сборки (Boot1 закоммичен намеренно). `Clear.cmd` удаляет `bin\`, `obj\`, `packages\` и пересоздаёт `bin\*\NuGetSource` из nupkg Nemerle — после него нужен NuGet restore.

## Стиль

`.editorconfig` обязателен: **CRLF, UTF-8 BOM, 2 пробела**. В репозитории файлы с русскими именами и временные файлы в корне (`diff.txt`, `text.txt`, `test-*.txt`) — не удалять и не перекодировать.
