forensic-scanner-interface-micro-fibers = Микроволокна
forensic-pad-verb-text = Получить образцы
forensic-pad-verb-message = Выполнить отбор образцов
forensic-pad-verb-no-sapmles = Объект не имеет образцов для взятия.
forensic-scanner-match-micro-fiber = Найдены совпадения по микроволокнам!
glove-print-scanner-verb-text = Собрать пробу сканером
glove-print-scanner-verb-message = Просканировать предмет в выбранном режиме и получить палочку с пробой.
glove-print-scanner-mode-surface = сбор микроволокон
glove-print-scanner-mode-own = образец предмета
glove-print-scanner-mode-label = Режим
glove-print-scanner-mode-verb = Выбрать режим
glove-print-scanner-mode-tooltip = Микроволокна — образцы перчаток, одежды и снаряжения на поверхности предмета. Образец — собственный индивидуальный код выбранной пары перчаток или предмета; предмет без образца вызовет отказ.
glove-print-scanner-mode-surface-short = Микроволокна
glove-print-scanner-mode-own-short = Образец
glove-print-scanner-mode-selected = Режим сканера: { $mode }.
glove-print-scanner-switch-hint = Используйте сканер в руке, чтобы открыть круговое меню выбора режима.
glove-print-scanner-no-own-print = Сканирование отклонено: у предмета нет собственного индивидуального образца.
glove-print-scanner-mode-changed = Сканирование прервано: режим сканера изменён.
glove-print-swab-unused = На палочке нет пробы. Готовые пробы выдаёт сканер микроволокон.
glove-print-swab-used = На палочке сохранена проба. Для её изучения вставьте её в анализатор микроволокон.
glove-print-swab-label = проба микроволокон: { $source }
glove-print-swab-source = Источник пробы: { $source }
glove-print-analyzer-slot-name = Проба микроволокон
glove-print-analyzer-interface-title = Анализатор микроволокон
glove-print-analyzer-interface-number = № { $number }
glove-print-analyzer-interface-number-caption = СЕРИЙНЫЙ
glove-print-analyzer-interface-number-caption-second-line = НОМЕР
glove-print-analyzer-interface-number-tooltip = Номер аппарата: { $number }
glove-print-analyzer-interface-brand = NT
glove-print-analyzer-interface-eject = Извлечь пробу
glove-print-analyzer-interface-print = Распечатать
glove-print-analyzer-interface-analyze = Провести анализ
glove-print-analyzer-interface-analyzing = Выполняется анализ образца…
glove-print-analyzer-interface-ready = Образец готов к анализу.
glove-print-analyzer-interface-analysis-interrupted = Анализ прерван: образец извлечён.
glove-print-analyzer-interface-description = Описание
glove-print-analyzer-interface-description-placeholder = Введите описание образца…
glove-print-analyzer-interface-save-description = Сохранить
glove-print-analyzer-interface-description-value = Описание: { $description }
glove-print-analyzer-interface-history = История
glove-print-analyzer-interface-compare-history = Сравнение образцов
glove-print-analyzer-interface-compare-first = Первый образец
glove-print-analyzer-interface-compare-second = Второй образец
glove-print-analyzer-interface-compare-run = Сравнить
glove-print-analyzer-interface-compare-instructions = Выберите два образца из истории и нажмите «Сравнить».
glove-print-analyzer-interface-compare-same-sample = Нужны два разных образца.
glove-print-analyzer-interface-compare-profile-count = Совпавшие коды образцов перчаток: { $count }
glove-print-analyzer-interface-compare-micro-fiber-count = Совпавшие коды образцов хранилищ: { $count }
glove-print-analyzer-interface-compare-no-profiles = Совпадений по кодам образцов перчаток нет.
glove-print-analyzer-interface-compare-no-micro-fibers = Совпадений по кодам образцов хранилищ нет.
glove-print-analyzer-interface-compare-note = Совпадение кода образца указывает на конкретную пару перчаток или экземпляр хранилища, но не устанавливает его владельца.
glove-print-analyzer-interface-compare-not-enough = Для сравнения нужны два исследованных образца в истории.
glove-print-analyzer-interface-current-sample = Неисследованный образец
glove-print-analyzer-interface-history-empty = История пуста.
glove-print-analyzer-interface-history-sample = Образец { $identifier }
glove-print-analyzer-interface-source = Исследуемый предмет: { $source }
glove-print-analyzer-interface-source-heading = { $source }
glove-print-analyzer-interface-analyzed-at = Время анализа: { $time }
glove-print-analyzer-interface-printer-ready = Копировальный аппарат подключён и готов к печати.
glove-print-analyzer-interface-printer-unavailable = Копировальный аппарат подключён, но не готов: проверьте питание, тонер и очередь печати.
glove-print-analyzer-interface-printer-disconnected = Нет связи с копировальным аппаратом. Установите связь мультитулом.
glove-print-analyzer-port-source-name = Отчёт анализа микроволокон
glove-print-analyzer-port-source-description = Отправляет заключение в связанный копировальный аппарат.
glove-print-analyzer-port-sink-name = Печать документов
glove-print-analyzer-port-sink-description = Принимает заключения связанного анализатора для печати.
glove-print-analyzer-report-title = Заключение экспертизы микроволокон
glove-print-analyzer-report-header = [bold]Форма НТ-СБ-ЭСП[/bold]
    { "[head=2]Заключение экспертизы микроволокон[/head]" }

    { "[bold]Регистрационный номер:[/bold]" } { $identifier }
glove-print-analyzer-report-source = [bold]Исследуемый предмет:[/bold] { $source }
glove-print-analyzer-report-analyzed-at = [bold]Время завершения анализа (от начала раунда):[/bold] { $time }
glove-print-analyzer-report-results = [head=3]Идентифицированные образцы перчаток[/head]
    { "[bold]Количество образцов перчаток:[/bold]" } { $count }
glove-print-analyzer-report-trace = [bold]Образец № { $number }[/bold]
    { "[bold]Вид перчаток:[/bold]" } { $type }
    { "[bold]Код образца:[/bold]" } { $code }
glove-print-analyzer-report-micro-fibers = [head=3]Идентифицированные образцы хранилищ[/head]
    { "[bold]Количество образцов хранилищ:[/bold]" } { $count }
glove-print-analyzer-report-micro-fiber = [bold]Образец хранилища № { $number }[/bold]
    { "[bold]Хранилище:[/bold]" } { $type }
    { "[bold]Код образца:[/bold]" } { $code }
glove-print-analyzer-report-description = [head=3]Описание образца[/head]
    { $description }
glove-print-analyzer-report-footer = [italic]Документ сформирован автоматически анализатором микроволокон.[/italic]

    { "[italic]Место для подписи и печати детектива[/italic]" }
glove-print-analyzer-interface-no-swab = Вставьте палочку с пробой.
glove-print-analyzer-interface-unused-swab = Вставленная палочка ещё не использована.
glove-print-analyzer-interface-traces = Образцы перчаток
glove-print-analyzer-interface-no-traces = Образцы перчаток не обнаружены.
glove-print-analyzer-interface-micro-fibers = Образцы хранилищ
glove-print-analyzer-interface-no-micro-fibers = Образцы хранилищ не обнаружены.
glove-print-analyzer-interface-type = Вид перчаток: { $type }
glove-print-analyzer-interface-item-type = Хранилище: { $type }
glove-print-analyzer-interface-code = Код образца: { $code }
glove-print-analyzer-interface-glove-sample-number = Образец перчаток № { $number }
glove-print-analyzer-interface-storage-sample-number = Образец хранилища № { $number }
glove-print-analyzer-interface-empty-category = Не обнаружены
glove-print-analyzer-interface-unknown-type = Неизвестный вид перчаток
glove-print-analyzer-interface-unknown-item = Неизвестное хранилище
