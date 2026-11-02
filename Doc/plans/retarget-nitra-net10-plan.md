# План: перевод `retarget-nitra` на nemerle-net10 (dotnet SDK 10)

Дата: 2026-10-31. Статус: **исполнен** (R0–R7 закрыты 2026-10-31; финальная приёмка: вход desktop-MSBuild, тесты 35/35+20/20, Stage2 ×4 зелёные, TDL-тройка подтверждена; отклонения — в «Решения» и чек-листе). Ветка: `retarget-nitra` (== `kl/retarget-nitra`, локальная проверена в `0/0`).
Материнские документы: `F:\RSDN\nemerle\doc\plans\nitra-tdl-retarget-plan.md` + `...-checklist.md` (цепочка приёмки **TDL → nitra → nemerle**; атомы N1.x соответствуют фазам R1–R7 ниже).

## Репозитории и пути-якоря (для новой сессии — читать в этом порядке)

| Что | Путь |
|---|---|
| nitra (рабочий репозиторий, ветка `retarget-nitra`) | `F:\RSDN\nitra` |
| nemerle-репозиторий (`retarget-compiler`; тулчейн-справка — `AGENTS.md` в корне) | `F:\RSDN\nemerle` |
| Дроп nemerle-тулчейна (версия `Nemerle.10.0.79.nupkg`) | `F:\RSDN\nemerle\bin\Debug\net-4.0` |
| Материнские план/чек-лист | `F:\RSDN\nemerle\doc\plans\nitra-tdl-retarget-plan.md`, `nitra-tdl-retarget-checklist.md` |
| Логи N0-проб (BuildBoot/Stage1, путь зелёного MSBuild) | `F:\RSDN\nemerle\.tmp\nitra-tdl\phaseN01\` (`probe1-buildboot.log`, `probe2-stage1.log`, `msbuild-path.txt`) |
| TDL (потребитель, вне скоупа nitra-атомов) | `F:\RSDN\TDL` (ветка Core: `retarget-tdl`, remote `kl`) |
| Этот план (статус/решения писать сюда же) | `F:\RSDN\nitra\Doc\plans\retarget-nitra-net10-plan.md` |
| Скретч/логи фаз R* | `F:\RSDN\nemerle\.tmp\nitra-tdl\phaseR0\`, `phaseR1\`, ... |
| Проверка синхронизации ветки | `git -C F:\RSDN\nitra rev-list --left-right --count retarget-nitra...kl/retarget-nitra` → `0 0`; HEAD `ec3759b1b` |

Ключевые файлы внутри nitra: вход `Build.cmd`/`Build-base.cmd` (корень); бутстрап `Common\BootTasks.proj` (цели `BuildBoot`/`ShiftBoot`, список `NccFile`); общие цели `Common\NitraBuild.targets`; сид `Boot1\Net-4.0\*` + `Boot1\version.xml` (=143); исходники бута `Boot2\{Nitra.Runtime,Nitra.Compiler,Nitra.Grammar,DotNetLang}\*.nproj` (отслеживаются в git!); SLN core-входа `Nitra-Stagt1.sln`; резолвинг тулчейна в каждом `.nproj`: property `Nemerle` = `Boot1\$(NemerleVersion)` при `Stage=Boot`, иначе `$(NemerleBinPathRoot)\Net-4.0` (`NemerleVersion=Net-4.0`, дефолт `NemerleBinPathRoot=%ProgramFiles%\Nemerle`). Харнесс тестов: `Tests\run-positive.cmd`/`run-negative.cmd` → `Tests\sub-env.cmd`/`sub-run.cmd`. Хэнд-off TDL: `Nitra.MSBuild.Sdk\Sdk.props` (свойство `NitraAndNemerleSourcesPath`, override-файл `Nitra.user`).

## Цель и приёмка

Nitra Core-линии собирается **новым компилятором Nemerle (ncc с хостом .NET 10)** и **на dotnet SDK 10** — без VS2017, VsDevCmd и реестровых пинов; возможности компилятора считаются прежними, т.е. исходники правятся минимально (то, что упёрлось в FX-only API).

Приёмка:
1. С чистого checkout: Boot → Stage1 без ошибок входной командой из `.NET 10 SDK` (`dotnet msbuild`; допустимый фолбэк — desktop MSBuild ≥18 от vswhere, если R0 докажет невозможность dotnet-host; фолбэк = зафиксированное отклонение).
2. `Tests\run-positive.cmd` / `run-negative.cmd` (корпуса `Tests\!Positive` 35, `Tests\!Negative` 20) — зелёные, числа в чек-лист.
3. Grammars + LanguageCompiler собраны; smoke: Calculator через Stage1-тулчейн.
4. Артефакты для TDL на месте (`Sdk.props`-тройка: `nitra\bin\Debug\Stage1`, `nitra\Grammars\bin\Debug`, nemerle-Stage1) — фактическими файлами.
5. Boot1/Boot2 перезасеваны новым тулчейном; сид — отдельный бинарный коммит после зелёного прогона.
6. Путь до nemerle-дропа **остался настраиваемым**: только `/p:NemerleBinPathRoot` / env / `Nitra.user`; в коммитимых файлах дерева nitra абсолютных путей `F:\RSDN\...` (и иных машинных) нет. Дефолт `%ProgramFiles%\Nemerle` в `.nproj` не менять — он переопределяется входом.
7. **Финальный гейт обязателен: Stage2 собран + тесты прогнаны повторно** (фаза R6) — без этого работа не считается принятой.

## Текущее состояние (проверено 2026-10-31)

| Факт | Детали |
|---|---|
| Дроп nemerle | `F:\RSDN\nemerle\bin\Debug\net-4.0` — net10-drop (FC ×10 зелёные): `ncc.exe` (apphost) + `ncc.dll/deps/runtimeconfig`, `Nemerle.MSBuild.targets`, `Nemerle.Sdk.props/targets`, `Nemerle.MSBuild.Tasks.dll` (NS2.0), `Nemerle[.Compiler/.Macros/.Peg/.Diff].dll`, `CSharpParser.dll`, `ncc.parser.csharp.dll`, `Nemerle.Compiler.Test.exe`, dnlib, фасадные `System.*`/`netstandard`. `ncc32/64.exe` **больше нет**. |
| Сборка дропа | `build.ps1` (нужен .NET 10 runtime; runtimeconfig закреплён на 10.0.0, roll-forward Minor). Потребителями nemerle **не пересобирается** — только готовые дропы. |
| `.nproj` в ветке | SDK-гибриды: `Import $(Nemerle)\Nemerle.Sdk.props/targets` (обёртки Microsoft.NET.Sdk, `LanguageTargets` → `Nemerle.MSBuild.targets`), `TargetFramework=netstandard20`, обязателен `/t:Restore`. `AfterBuild` (Policy01/NugetPack) закомментирован → земляные мины `Common\NitraBuild.targets` (`MSBuild.Community.Tasks` net40-сборка, `AL`, `Exec nuget.exe`) **спят**. |
| `Nemerle.MSBuild.targets` | FX-only задач (`GetFrameworkSdkPath`, `Al`, `Exec`) не содержит; компиляция через `UsingTask Nemerle.Tools.MSBuildTask.Ncc` (NS2.0). Шансы на `dotnet msbuild` — высокие, подтверждается spikes R0. |
| Entry-скрипты | `Build.cmd` → `Build-base.cmd`: VS2017-реестровый пин (`SxS\VS7\15.0` + VsDevCmd) — обходится прямым msbuild + `MSBUILDENABLEALLPROPERTYFUNCTIONS=1`. N0-проба: desktop `MSBuild\Current\Bin\MSBuild.exe` (VS 18) — зелёный прогон BuildBoot. |
| `Boot1\Net-4.0` | Закоммичен **FX-сид прошлого поколения**: есть `ncc32/ncc64.exe`, нет `Nemerle.Sdk.props/targets`, нет apphost-набора. `Stage=Boot` разрешает `$(Nemerle)` в `Boot1\Net-4.0` и безусловно импортирует `$(Nemerle)\Nemerle.Sdk.props` → **чистый checkout сегодня даёт BuildBoot red (MSB4019)**. |
| N0-пробы (nemerle-сторона) | С локально подложенным в Boot1 новым тулчейном: BuildBoot зелёный (выход `Boot2\Net-4.0\Nitra.*.Boot143.dll`); Stage1 за ~6 мин — ровно 3 ошибки в 2 проектах: `Nitra.ClientServer.Server` (`Resources.resx`-макро FX-only + unbound `PrettyPrintDoughnut`) и `Ide\NitraCommonVSIX\Vsix2017` (packages.config-легаси, вне скоупа). Логи: `F:\RSDN\nemerle\.tmp\nitra-tdl\phaseN01\`. |
| `Common\BootTasks.proj` | `ShiftBoot` копирует в сид несуществующие `ncc32/ncc64.exe` (блокер перезасевки) и **не** копирует `ncc.dll/deps/runtimeconfig`, `Nemerle.Sdk.*`. Сам `BuildBoot` копирования файлов не делает → текущий BuildBoot это не задевает. |
| Тесты | `Tests\run-positive.cmd`/`run-negative.cmd`: харнесс `%Nemerle%\Nemerle.Compiler.Test.exe` из дропа, `-ref bin\Debug\Stage1\Nitra.Runtime.dll`, `-macro Nitra.Compiler.dll`, копирует `Grammars\Bin\%Config%`; env `NemerleBinPathRoot` обязан указывать на `<nemerle>\bin\Debug`. |

## Вход: команды (копи-паста)

```bat
rem === окружение (машинное, НЕ коммитить) ===
set NemerleBinPathRoot=F:\RSDN\nemerle\bin\Debug
set MSBUILDENABLEALLPROPERTYFUNCTIONS=1
cd /d F:\RSDN\nitra

rem === вариант A: dotnet SDK 10 host (целевой, проверяется в R0) ===
dotnet msbuild Common\BootTasks.proj /t:BuildBoot /p:NemerleBinPathRoot=%NemerleBinPathRoot%
dotnet msbuild Nitra-Stagt1.sln /t:Restore /p:NemerleBinPathRoot=%NemerleBinPathRoot%
dotnet msbuild Nitra-Stagt1.sln            /p:NemerleBinPathRoot=%NemerleBinPathRoot%

rem === вариант B: desktop MSBuild >=18 (зелёный путь N0-пробы) ===
rem N0-проба использовала: C:\Program Files\Microsoft Visual Studio\18\Professional\MSBuild\Current\Bin\MSBuild.exe
for /f "usebackq delims=" %%i in (`"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe`) do set "MSB=%%i"
"%MSB%" Common\BootTasks.proj /t:BuildBoot /p:NemerleBinPathRoot=%NemerleBinPathRoot%
"%MSB%" Nitra-Stagt1.sln /t:Restore /p:NemerleBinPathRoot=%NemerleBinPathRoot%
"%MSB%" Nitra-Stagt1.sln            /p:NemerleBinPathRoot=%NemerleBinPathRoot%
```

Сигнатуры известных красных ошибок (из `F:\RSDN\nemerle\.tmp\nitra-tdl\phaseN01\probe2-stage1.log`, чтобы матчить без переисследования):
1. `Nitra\ClientServer\Nitra.ClientServer.Server\Properties\AssemblyInfo.n(5): error : The Resource(".../Properties/Resources.resx") macro is not supported when the compiler runs on .NET (Core): ResXResourceReader (System.Windows.Forms) is Framework-only.`
2. `Nitra\ClientServer\Nitra.ClientServer.Server\HtmlPrettyPrintWriter.cs(201): error : unbound name 'PrettyPrintDoughnut' in 'Resources'` — следствие п.1 (макро не сгенерировал класс `Resources`).
3. `Ide\NitraCommonVSIX\Vsix2017\NitraCommonVSIX.csproj(394): error : ... missing file ..\..\..\packages\Microsoft.VisualStudio.SDK.EmbedInteropTypes.15.0.17\build\Microsoft.VisualStudio.SDK.EmbedInteropTypes.targets` — VSIX-легаси, вне скоупа.

## Несовпадения дропа с ожиданиями ветки (инвентарь)

1. `ncc32.exe`/`ncc64.exe` отсутствуют (только `ncc.exe`-apphost + `ncc.dll`).
2. `ncc.exe` из дропа неработоспособен без соседних `ncc.dll`, `ncc.deps.json`, `ncc.runtimeconfig.json` → должны попасть в сид.
3. `Boot1` не содержит `Nemerle.Sdk.props/targets` (новые импорты nproj) — первопричина MSB4019.
4. Ресурсы: `Resource(...resx)`-макро недоступен в Core-ncc (`ResXResourceReader` из `System.Windows.Forms`) — единственная найденная FX-only-зависимость исходников core-набора.
5. VSIX/IDE (`Ide\NitraCommonVSIX\*`) — packages.config-легаси, не мигрирован; **вне скоупа** (IDE-линия = master).
6. Хост-окружение: .NET 10 runtime; отсутствие VS2017 ломает `Build-base.cmd` (обход — R0).
7. Потенциально (проверить в R0/R3): `MSBUILDENABLEALLPROPERTYFUNCTIONS=1` для `BootTasks.proj`; поведение `Ncc`-таска под `dotnet msbuild`; `GetGitTagRevision` под dotnet-host (NS2.0, должен работать).

## Фазы

### R0 — Spike входа на dotnet SDK 10 (без коммитов; ~½ дня)
R0.1 `dotnet msbuild System.Collections.Immutable.Light\System.Collections.Immutable.Light.csproj /t:Restore;Build` — базовая совместимость SDK-проекта.
R0.2 `dotnet msbuild Boot2\Nitra.Runtime\Nitra.Runtime.nproj /t:Restore` и затем `/t:Build` с бутстрап-свойствами (после ручной подложки файлов в `Boot1`, см. R1.1; полный эталон свойств — `BuildProperties` в `Common\BootTasks.proj`): `/p:Configuration=Debug /p:Stage=Boot /p:NameSuffix=BootSpike /p:BuildConstant=BOOT /p:OutputPath=F:\RSDN\nitra\Boot2\Net-4.0\ /p:CommonFilesPath=F:\RSDN\nitra\Common\ /p:SCITargetFramework=net45 /p:NemerleBinPathRoot=%NemerleBinPathRoot%` — проверяет полный путь `Nemerle.Sdk` → `Nemerle.MSBuild.targets` → `Ncc`-таск под dotnet-host; зафиксировать любые MSB4803/FX-task промахи.
R0.3 `dotnet msbuild Common\BootTasks.proj /t:BuildBoot` с `MSBUILDENABLEALLPROPERTYFUNCTIONS=1` (в dotnet-хосте property functions включены по умолчанию — проверить).
R0.4 Альтернатива-фолбэк: тот же прогон desktop-MSBuild ≥18 (vswhere) — у N0-пробы зелёный.
Выход: задокументированная зелёная команда входа (решение dotnet-host vs desktop-host + список правок для R1/R2) — дописать в «Решения» этого файла.
Гейт: команда задокументирована и зелёна на Spike-мишени.

### R1 — Перезасевка Boot1 (аналог nemerle-фазы 4.6; атомы N1.7+N1.8)
R1.1 Локально (не коммитя) подложить в `Boot1\Net-4.0` из дропа: `Nemerle.Sdk.props/targets`, `Nemerle.MSBuild.targets`, `Nemerle.MSBuild.Tasks.dll`, `ncc.exe/dll/deps/runtimeconfig`, `Nemerle.dll`, `Nemerle.Compiler.dll`, `Nemerle.Macros.dll`, `Nemerle.Peg.dll`, `CSharpParser.dll`, `ncc.parser.csharp.dll`, `dnlib.dll`, `System.CodeDom.dll`, фасадные `System.*`/`netstandard` — прогнать `BuildBoot` (Boot2 → `Nitra.*.Boot143.dll`).
R1.2 Прогнать `Nitra-Stagt1.sln` Stage1 (ожидаем только resx-красный `ClientServer.Server` — см. R2; VSIX исключаем пролётом).
R1.3 Исправить `Common\BootTasks.proj` (`NccFile`): убрать `ncc32/ncc64`; добавить apphost-набор (`ncc.dll`, `ncc.deps.json`, `ncc.runtimeconfig.json`) и `Nemerle.Sdk.props/targets`; сверить `System*.dll`/`Microsoft*.dll`/`netstandard.dll` с фактическим содержимым дропа.
R1.4 `ShiftBoot` — во временном worktree (не в основном дереве): промоция Boot2→Boot1, версия `Boot1\version.xml` 143→144, пересборка:
```bat
git -C F:\RSDN\nitra worktree add --detach F:\RSDN\nitra-shift retarget-nitra
rem в worktree: сначала R1.1 (подложка сида) + BuildBoot, затем:
"%MSB%" Common\BootTasks.proj /t:ShiftBoot /p:NemerleBinPathRoot=%NemerleBinPathRoot%
```
Цель `ShiftBoot` сама копирует `Boot2→Boot1`, инкрементит версию, переносит свежие `Nitra\*` в `Boot2\*` и пересобирает (`CallTarget BuildBoot`). `xcopy /EXCLUDE:excludelist.txt` резолвится от каталога цели — файл лежит в `Common\excludelist.txt`. Корневой `ShiftBoot.cmd` не использовать (FX-наследие с VS2017-пином).
R1.5 Сид-коммит: обновлённые бинари `Boot1/Net-4.0/*` + `Boot1/version.xml` + синхронные исходники `Boot2/*` — **отдельный бинарный коммит**.
Гейт: зелёный прогон с чистого checkout после сид-коммита (BuildBoot → Stage1 → тесты R4).

### R2 — `Nitra.ClientServer.Server` resx-макро (N1.3)
Файлы: `Nitra\ClientServer\Nitra.ClientServer.Server\Properties\AssemblyInfo.n` (строка 5, `Resource(...Resources.resx)`) и `Nitra\ClientServer\Nitra.ClientServer.Server\HtmlPrettyPrintWriter.cs` (строка 201, `Resources.PrettyPrintDoughnut`); сам ресурс — `Nitra\ClientServer\Nitra.ClientServer.Server\Properties\Resources.resx`.
Решение по умолчанию: **порт** — убрать макро, `PrettyPrintDoughnut` → константа/литерал. Альтернатива: исключение `ClientServer`-проектов из `Nitra-Stagt1.sln` core-состава (дешевле, но гадость для IDE-линии).
Гейт: проект компилируется ncc-net10 или вычеркнут с записью в отклонения.

### R3 — Core-сборка zero-error (N1.4)
Определить core-набор сборки (Stage-цепочка + Grammars + LanguageCompiler; VSIX/IDE — вне) и собрать входной командой R0 без ошибок.
Гейт: zero-error полный билд; время/выходы в журнал.

### R4 — Тесты: baseline (N1.5)
Предварительно: собраны Stage1 (`bin\Debug\Stage1`) и Grammars (`Grammars\Bin\Debug`). Прогон:
```bat
cd /d F:\RSDN\nitra
set NemerleBinPathRoot=F:\RSDN\nemerle\bin\Debug
set Configuration=Debug
Tests\run-positive.cmd    rem корпус Tests\!Positive, 35 файлов
Tests\run-negative.cmd    rem корпус Tests\!Negative, 20 файлов
```
Артефакты прогона: `Tests\Bin\Debug\Positive|Negative` (сравнение с golden — по коду `sub-run.cmd`).
Гейт: pass/fail-числа в чек-лист; если что-то красно — дефект тулчейна чинится атомом в nemerle (гейты FC ×10 + сьют), не корпусом.

### R5 — Grammars + LanguageCompiler smoke (N1.6)
Calculator через Stage1-тулчейн (`Grammars\Samples\Calculator\Sample.Calc`, `Sample.Calc.App`). Заодно проверить актуальность закоммиченных метаданных Stage1: `Nitra\Nitra.Runtime\Nitra.Runtime.Stage2.dll.nitrametadata2`, `Nitra\DotNetLang\DotNetLang.Stage2.dll.nitrametadata2` — после пересборки новым тулчейном они устаревают; перегенерация — корневым `UpdateStage1Metadata.cmd` (копирует из `obj\Debug\Stage2\`), результат коммитить (см. решение Р1.5).
Гейт: smoke-сборка зелёная.

### R6 — ФИНАЛЬНЫЙ ГЕЙТ: Stage2 + повторный прогон тестов
Самохостинг-доказательство: `*.Stage2.nproj` собирают те же исходники **тулчейном из дропа** (`Nemerle = $(NemerleBinPathRoot)\Net-4.0`), ссылаясь на артефакты Stage1; выход — `bin\Debug\Stage2\Nitra.*.Stage2.dll`.
```bat
rem RunDebugger=false обязателен: у части Stage2-проектов RunDebugger=true в nproj → ncc -debugger →
rem Debug.Assert в headless-вызове (находка R5.2, логи phaseR5\).
for %%p in (Nitra.Runtime Nitra.Compiler Nitra.Grammar DotNetLang) do (
  "%MSB%" Nitra\%%p\%%p.Stage2.nproj /t:Restore /p:NemerleBinPathRoot=%NemerleBinPathRoot%
  "%MSB%" Nitra\%%p\%%p.Stage2.nproj           /p:NemerleBinPathRoot=%NemerleBinPathRoot% /p:RunDebugger=false
)
```
Затем — **повторный полный прогон** `Tests\run-positive.cmd` / `run-negative.cmd` (контур харнесса по `sub-env.cmd` работает на Stage1 — это штатно; Stage2-проход проверяет, что Stage1-компилятор корректно пересобирает себя и окружение не «живёт» устаревшими Stage1-бинарями).
Гейт: четыре Stage2-проекта без ошибок + тесты повторно зелёные с числами не хуже baseline R4; результаты — в журнал и чек-лист. Это последний исполняемый гейт перед закрытием плана.

### R7 — Handoff и документация (N1.9 + N3)
Подтвердить три выхода для TDL фактическими артефактами (ожидается резолвинг `Nitra.MSBuild.Sdk\Sdk.props` при `NitraAndNemerleSourcesPath=F:\RSDN\`): `F:\RSDN\nitra\bin\Debug\Stage1\` (`Nitra.Runtime.dll`, `Nitra.Compiler.dll`, `Nitra.Grammar.dll`, `DotNetLang.dll`, `*.nitrametadata2`), `F:\RSDN\nitra\Grammars\Bin\Debug\`, `F:\RSDN\nemerle\bin\Debug\net-4.0\Stage1\`. Обновить AGENTS.md этой ветки (если R0 выбрал другой вход) и чек-лист материнского плана (`F:\RSDN\nemerle\doc\plans\nitra-tdl-retarget-checklist.md`); отчёт-резюме со списком отклонений.
Гейт: N2-фаза TDL может стартовать.

## Правила исполнения (наследуются из материнского плана)

- Один атом за раз; **один билд во всей системе** параллельно (nemerle-фазы/N2 не пересекаются по времени).
- Скретч/логи: `F:\RSDN\nemerle\.tmp\nitra-tdl\phaseR*\` (продолжая нумерацию `phaseN0*`).
- В nemerle потребители nemerle **не пересобирают** — только дроп; баг компилятора → отдельный атом в nemerle.
- Путь до nemerle — машинный, настраивается только окружением/properties (приёмка п.6). `F:\RSDN\TDL` (Core-ветка `retarget-tdl`, remote `kl`) — домен фазы N2 родительского плана, в nitra-атомы не вмешивать.
- Ничего не пушим (`kl`/`github` — только явное решение пользователя). `master`/`test` не трогать; untracked `AI/`, `diff.txt` и пр. — не трогать.
- Правки nproj/targets — точечные; стили `.editorconfig` (CRLF, BOM, 2 spaces).

## Риски / открытые вопросы

1. **dotnet-host vs desktop-host (главный, решает R0)**: `Nemerle.MSBuild.targets` чист от FX-only задач, но полный путь (SDK-импорты, `GetAssemblyIdentity`, `UsingTask` NS2.0, `GenerateAssemblyInfo`) под `dotnet msbuild` не проверялся. Блокер внутри nemerle-owned файлов = атом в nemerle; временный фолбэк — desktop MSBuild ≥18 (задокументированное отклонение).
2. **Совместимость FX-макро-сборок в сиде**: первый BuildBoot грузит `Boot1\Nitra.Compiler.dll` прошлого поколения; N0-проба доказала зелёный путь с локально подложенным дропом — механику переносим как есть (R1.1), при внезапном red-fail — разбирать `Stage=Boot`-резолвинг MacroReference-ссылок.
3. `*.Stage1.dll.nitrametadata2`/Stage2-метаданные в дереве (`Nitra\Nitra.Runtime`, `Nitra\DotNetLang`) после пересборки новым тулчейном изменятся — попадут в сид-коммит R1.5 или отдельный коммит (зафиксировать выбор).
4. VSIX-плагины на этой ветке навсегда красные или вычеркиваются из sln — зафиксировать (по умолчанию: вне core-состава, не чинить).
5. `Nemerle.Compiler.Test.exe` под dotnet-хостом требует запуска exes через `-r dotnet -rp exec` (см. nemerle AGENTS) — для nitra-корпусов используется компиляция без запуска (`-output:`), проверить, что `-r`-дефолты (6.2b) достаточны.

## Решения (заполняется по ходу)

- [Р0] host: **desktop MSBuild ≥18** — `C:\Program Files\Microsoft Visual Studio\18\Professional\MSBuild\Current\Bin\MSBuild.exe` (18.11, VS18 Pro) — 2026-10-31, логи: `F:\RSDN\nemerle\.tmp\nitra-tdl\phaseR0\` (`R0.2-nproj-dotnet.log`, `R0.3-buildboot-dotnet.log`, `R0.4-buildboot-desktop.log`, `R0.4c-E1-n01-green.log`, `R0.4c-E1b-n01boot1-newdroprefs.log`, `R0.4c-E4-interface.txt`).
  - `dotnet msbuild` для boot/nproj невозможен: в nemerle-owned `Nemerle.MSBuild.targets` (стр. ~257–269) by-design guard-цель `ErrorOnMacroReferenceUnderCoreHost` → `MacroReference is not supported when building with the .NET (Core) MSBuild host` при непустом `@(MacroReference)` и `MSBuildRuntimeType==Core` (логи R0.2/R0.3). Это плановый фолбэк (зафиксированное отклонение); dotnet SDK 10.0.402 установлен и применим для остального.
  - R1: seed-only `BuildBoot` на desktop-хосте с `NemerleBinPathRoot=F:\RSDN\nemerle\bin\Debug` (текущий дроп 10.0.79) зелёный на нетронутом FX-сиде (`N01` worktree, логи `R0.4c-E1-n01-green.log`, `R0.4c-E1b-n01boot1-newdroprefs.log`); выходы `Boot2\Net-4.0\Nitra.*.Boot143.dll` линкуют Nemerle 10.0.79 и реализуют `IParser.Parse(SourceSnapshot)` ⇒ **подложка бинарей дропа в Boot1 (R1.1) не нужна — план устарел/вреден**.
  - R1.3/ShiftBoot: подмешивание бинарей текущего дропа в FX-сид ломает ncc (v10 грузит макро-сборки поколения Nemerle 1.2): TypeLoad `Method 'Parse' in type 'Nitra.Compiler.Integration.NitraParser' ... does not have an implementation` (`IParser.Parse(ISource)` old vs `Parse(SourceSnapshot)` new; `R0.4-buildboot-desktop.log`, `R0.4c-E4-interface.txt`). Правило: состав `Boot1` — одного поколения; промоция Boot2→Boot1 — только вместе с компиляторным бандлом дропа (R1.3 `NccFile` + ShiftBoot).
  - Машинный блокер (от хоста не зависит): gitignored корневой `packages\Microsoft.Net.Compilers.2.10.0` (легаси-пин Csc из `Directory.Build.props`) ломает любой csproj-билд (MSB4064/MSB4063); на чистом checkout отсутствует. На этой машине переименован в `packages\Microsoft.Net.Compilers.2.10.0.disabled-spike-r0` и **остаётся переименованным до конца R1–R7**; `Directory.Build.props` не править.
  - R2/R3: для boot-стадийных `.nproj` в `/t:Restore` нести те же boot-свойства, что и в сборке (`SCITargetFramework=net45` и т. п.), иначе NETSDK1005 (`R0.2-nproj-dotnet.attempt1.log`).
  - Под desktop MSBuild держать env `MSBUILDENABLEALLPROPERTYFUNCTIONS=1` (`BootTasks.proj`, ToolsVersion 4.0); под dotnet-хостом не требовалось (ныне неактуально).
- [Р2] resx: **порт** — 2026-10-31, `Resource(...Resources.resx)`-макро убрано из `Properties\AssemblyInfo.n` (`Macro.Settings` сохранён), `Resources.PrettyPrintDoughnut` в `HtmlPrettyPrintWriter.cs` заменён `private const` verbatim-шаблоном (байт-в-байт `PrettyPrintDoughnut.htm`); `Properties\Resources.resx` и запись `EmbeddedResource` в `.nproj` оставлены как есть для FX-линии. Проверено инкрементальным прогоном `Nitra-Stagt1.sln` ncc-net10 (`R2.2-stage1-build.log`: `Nitra.ClientServer.Server -> bin\Debug\Stage1\Nitra.ClientServer.Server.exe`, exe 110592 б. 31.10 18:27:36; множество ошибок = только Ide\*/VSIX-легаси CS1747/CS1759, resx-ошибок нет).
- [Р1.5] метаданные-файлы: **отдельным коммитом в R5** — `*.Stage1.dll.nitrametadata2` устаревают после полной пересборки Stage1 (не было на момент сид-коммита `3284965eb`), перегенерация `UpdateStage1Metadata.cmd` и коммит — в атоме R5. Сид-коммит R1.5 содержит `Boot1/Net-4.0/*` (125 файлов, поколение 10.0.79) + `Boot1/version.xml` (144) + ресинк `Boot2/*` (74 файла, ресинк Boot2-снапшота сделан самим ShiftBoot) — дата 2026-10-31.

- [Р4] тесты baseline → финал: старт 1/35+17/20, все красные разобраны research-атомами (вердикты `F:\RSDN\nemerle\.tmp\nitra-tdl\phaseR4\R4.1-verdict.md`, `R4.4-verdict.md`), чинились: харнесс `-r dotnet -rp exec` (`d46230774`), харнесс `-ref` на shared-fw `System.Private.Xml.Linq.dll` (`83c6bb512`, work-around дефекта ncc — implicit-refs скипает `System.Private.*`; nemerle-атом внешний), spacer-блок `extend token IgnoreToken` в 9 тест-грамматиках (`762799b5e`), порт `Json.Grammar` в SDK-hybrid + spacer (`424a23645`), порт `Sample.Calc`/`Sample.Num` + spacer (`173931cc1`), актуализация ожидания `DeclarationCannotMap` (`81a9a79e7`), nitra-фикс NRE `Style.ForegroundColor` Option-проекцией (`4c86bfd8d`). **Финал: Positive 35/35, Negative 20/20 («All tests passed.»)** после rebuild `Nitra-Core.slnf`. Группа E (пробелы) и D1 — устаревший корпус/грамматики (провал воспроизведён на FX-бинарях, не регрет ретаргета), правки корпуса — зафиксированное обоснованное отклонение.

## Чек-лист статуса

- [x] N1.1 локальная `retarget-nitra` синхронизирована с `kl/retarget-nitra` (0/0, HEAD `ec3759b1b`)
- [x] R0 spike входа (dotnet SDK 10 vs desktop) — отчёт в «Решения»
- [x] R1 перезасевка Boot1 (сид `3284965eb`, version 144; гейт «чистый checkout» доказан на R3)
- [x] R2 resx-макро `ClientServer.Server` (порт, `fcaa9ff31`)
- [x] R3 core-сборка zero-error (`Nitra-Core.slnf`, `c3e0bd2dc`)
- [x] R4 тесты 35+20 — финал 35/35 + 20/20 (см. [Р4])
- [x] R5 Grammars + LanguageCompiler smoke (драйвер-Calculator через Stage1; метаданные `17ed03588`; sln/slnf-регистрация `8da140011`)
- [x] R6 ФИНАЛЬНЫЙ ГЕЙТ: Stage2 (4 проекта, `/p:RunDebugger=false`) + повторный прогон тестов — 35/35 + 20/20, логи `phaseR6\` (инцидент с рассинхроном версий 159/160 устранён re-sync билдом `Nitra-Core.slnf`)
- [x] R7 TDL-handoff + доки/AGENTS синхронизированы — три TDL-выхода подтверждены артефактами (`1.10.160.0` согласованы; сайдкары метаданных в Stage1 не нужны — embed-ресурс `Nitra.Metadata`, committed-сайдкары R5.2 `17ed03588`); AGENTS.md и материнский чек-лист обновлены (детали: прогресс-R7)
