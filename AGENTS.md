# AGENTS.md — Nitra (ветка `retarget-nitra`)

Форк [rsdn/nitra](https://github.com/rsdn/nitra) + внутренний TFS (`kl` = Cons-TDL). Эта ветка — **Core-линия nitra**: ядро (Nemerle, `.n`/`.nproj`, dnlib-бэкенд `DotNet.BackEnd.Dnlib/`) собирается новым компилятором Nemerle с хостом **.NET 10**. Ветки `master`/`test` — линия .NET Framework / VS2017 (свой AGENTS.md); ретаргет-правки в них не носить, и обратно FX-частное сюда тоже.

## Зависимости и раскладка

- Тулчейн: nemerle @ `retarget-compiler`, установленный drop `F:\RSDN\nemerle\bin\Debug\net-4.0` (`C:\RSDN` — симлинк на `F:\RSDN`). `ncc.exe` — **.NET 10 apphost**: нужен установленный .NET 10 рантайм (runtimeconfig закреплён на `Microsoft.NETCore.App 10.0.0`, roll-forward Minor — на 11.x-only машине ncc не стартует).
- **Nemerle лежит не в `%ProgramFiles%\Nemerle`, а соседним каталогом — путь обязан оставаться настраиваемым** (`/p:NemerleBinPathRoot=...` / env `NemerleBinPathRoot`, для nitra-проектов также `Nitra.user`). Абсолютные пути конкретной машины в коммитимые скрипты не вносить.
- Имя папки дропа `net-4.0` — легаси-макет, сохранён намеренно; библиотеки дропа — netstandard2.0, проекты nitra таргетят `netstandard20` через обёртку `$(Nemerle)\Nemerle.Sdk.props/targets` (`LanguageTargets` → `Nemerle.MSBuild.targets`).
- `.nproj` стали SDK-гибридами: перед сборкой обязателен `/t:Restore` (это делает `Build-base.cmd`; при ручном msbuild — отдельным проходом).
- Низовой потребитель — **TDL** (DSL тестирования, `F:\RSDN\TDL`, отдельный репозиторий с remote `kl`; Core-ветка `retarget-tdl`): подключается к nitra через `Nitra.MSBuild.Sdk/Sdk.props` — `NitraAndNemerleSourcesPath` → `nitra\bin\Debug\Stage1`, `nitra\Grammars\bin\Debug`, `nemerle\bin\Debug\net-4.0\Stage1`; локальный override — файл `Nitra.user` выше дерева проектов (в репозитории его нет — не коммитить, он и есть место для машинных путей).

## Сборка

- Точка входа Core-линии: `Build-Core.cmd` (`vswhere -latest` → MSBuild VS2019+/VS18, без VS2017-пина и VsDevCmd; `MSBUILDENABLEALLPROPERTYFUNCTIONS=1`; `NemerleBinPathRoot` — из env-переменной, иначе sibling-дроп `..\Nemerle\bin\<Config>`; шаги `Common\BootTasks.proj /t:BuildBoot` → `Nitra-Core.slnf` Restore+Build). `Build-Core-wo-boot.cmd` — без пересборки бутстрапа. Полный `Nitra-Stagt1.sln` — FX-наследие, в Core-работах не использовать.
- Точка входа (FX-линия, легаси; полный `Nitra-Stagt1.sln` — FX-наследие, для Core-работ не использовать): `Build.cmd` (= `Build-base.cmd Debug Build` → `Common\BootTasks.proj /t:BuildBoot` → `Nitra-Stagt1.sln` Restore+Build). `Build-wo-boot.cmd` — без пересборки бутстрапа.
- **Скрипты FX-линии (`BuildBoot.cmd`, `RebuildBoot.cmd`, `ShiftBoot.cmd`, `test.cmd`, `Clear.cmd`) на этой ветке не использовать** — они из master-линии про net40/Boot1-FX.
- `Build-base.cmd` содержит пин на VS2017 (реестр `...\SxS\VS7\15.0` + VsDevCmd). На машинах без VS2017 — воспроизводить шаги входа напрямую современным MSBuild (`vswhere -latest`), с `set MSBUILDENABLEALLPROPERTYFUNCTIONS=1` (иначе `BootTasks.proj` с ToolsVersion 4.0 не раскрывает property functions) и `/p:NemerleBinPathRoot=<nemerle>\bin\Debug`. Проверено N0-пробой: `MSBuild\Current\Bin\MSBuild.exe` (VS 18) — зелёный прогон.
- Цепочка: `BuildBoot` (компилирует `Boot2\*.nproj` toolset'ом из `Boot1\Net-4.0`, выход `Boot2\Net-4.0`) → `Nitra-Stagt1.sln` Stage1 (`bin\Debug\Stage1\`) → Stage2-проекты (`*.Stage2.nproj`).
- `Boot1\Net-4.0` — **закоммиченный бинарный сид**, перезасеван нет10-поколением (apphost-бандл `ncc.*`, `Nemerle.Sdk.*`, Nemerle 10.0.79; `Boot1\version.xml` = 144; сид-коммит `3284965eb`) → чистый checkout даёт зелёный BuildBoot. Бинари разных поколений в сиде не смешивать (v10 ncc не грузит FX-макро-сборки — TypeLoad на `IParser.Parse`).
- `Common\BootTasks.proj` (список `NccFile`) сверен с Core-host (`d2cfeccfa`): копирует `ncc.dll + ncc.deps.json + ncc.runtimeconfig.json`, `Nemerle.Sdk.*`. Будущие перезасевки: промоция `Boot2→Boot1` через `Common\BootTasks.proj /t:ShiftBoot` во временном worktree, сид — отдельный бинарный коммит после зелёного прогона (корневой `ShiftBoot.cmd` — FX-наследие, не использовать).
- Stage2-проекты (`*.Stage2.nproj`) собирать с `/p:RunDebugger=false`: у `Nitra.Runtime.Stage2.nproj` в nproj стоит `RunDebugger=true` → ncc `-debugger` → краш `Debug.Assert` в headless-вызове (находка R5.2, логи `phaseR5\`).
- После любого коммита предпочтительно сначала пересобирать `Nitra-Core.slnf` (входной билд Core-набора), а уже потом одиночные nproj: версии сборок git-производные (`Common\NitraBuild.targets`), и одиночная сборка `*.Stage2.nproj` перетягивает по графу зависимостей Stage1, но не Grammars → рассинхрон версий (`bin\Debug\Stage1` = 160 против `Grammars\Bin\Debug` = 159) ломает тестовый харнесс (`FileNotFoundException: Nitra.Runtime`, инцидент R6).

## Тесты

- `Tests\run-positive.cmd` / `Tests\run-negative.cmd` — основной контур: корпус `Tests\!Positive` (35 файлов) и `Tests\!Negative` (20), харнесс `Nemerle.Compiler.Test.exe` из nemerle-дропа.
- Перед прогоном: `set NemerleBinPathRoot=F:\RSDN\nemerle\bin\Debug` (дефолт `%ProgramFiles%\Nemerle` не найдётся) и собраны `bin\Debug\Stage1` + `Grammars\Bin\Debug` (харнесс копирует выходы из второго и линкует `-ref:Nitra.Runtime.dll -macro:Nitra.Compiler.dll`).
- **Статус Core-линии: ЗЕЛЁНЫЙ** — Positive 35/35, Negative 20/20 (прогон 31.10.2026, логи `F:\RSDN\nemerle\.tmp\nitra-tdl\phaseR6\`).
- Харнесс содержит два Core-workaround'а: `-r dotnet -rp exec` (launch-режим для `Nemerle.Compiler.Test.exe`, `d46230774`) и явный `-ref` на shared-framework `System.Private.Xml.Linq.dll` (`83c6bb512`: ncc-implicit-refs скипает `System.Private.*` под Core-host, иначе `XElement` нерезолвится; фикс на nemerle-стороне ожидает, отдельный атом).
- Golden-наборы `Tests\Visualizer\*.nsln` + `Nitra.TestsLauncher` и `Tests\CSharp.Grammar.Test` — легаси FX-линии, в Core-контуре не гонять.

## Известные красные точки (актуально на 31.10.2026)

- VSIX/IDE (`Ide\NitraCommonVSIX\*`, packages.config-based) — не собирается (нет пакетов) и **вне скоупа** Core-линии; FX/IDE-плагины живут на master.
- `Sample.Calc.App` — интерактивный by design: читает `Console.CursorLeft/Top`, которые швыряют `IOException` при перенаправлении ручек (stdin/stdout); неинтерактивный smoke Calculator'а — через драйвер, скомпилированный Stage1-тулчейном (план R5.1b, логи `phaseR5\`).
- Закрыто в ходе ретаргета (R1–R5): `Nitra.ClientServer.Server` resx-макро — портирован, инлайн шаблона (`fcaa9ff31`); `Json.Grammar`/`Sample.Calc`/`Sample.Num` — портированы и зарегистрированы в `Nitra-Stagt1.sln`+`Nitra-Core.slnf` (`424a23645`, `173931cc1`, `8da140011`), собираются входным билдом; перезасевка Boot1 выполнена (версия 144, нет10-поколение, сид-коммит `3284965eb`).

## Правила

- `github` — апстрим, не пушить; `kl` (TFS) — только по явному решению пользователя. Ветка: `retarget-nitra` (== `kl/retarget-nitra`).
- Untracked `AI/`, `diff.txt`, `text.txt`, `test-*.txt` — не трогать, не коммитить.
- `.editorconfig` обязателен: CRLF, UTF-8 BOM, 2 пробела.
- План и статус работ: `Doc/plans/retarget-nitra-net10-plan.md`; материнские план/чек-лист — в nemerle-репозитории, `doc/plans/nitra-tdl-retarget-{plan,checklist}.md` (цепочка приёмки TDL → nitra → nemerle).
