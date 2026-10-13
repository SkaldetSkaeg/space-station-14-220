forensic-scanner-interface-storage-micro-fibers = Микроволокна
forensic-pad-verb-text = Получить образцы
forensic-pad-verb-message = Выполнить отбор образцов
forensic-pad-verb-no-sapmles = Объект не имеет образцов для взятия.
forensic-scanner-match-storage-micro-fiber = Найдены совпадения по микроволокнам!
glove-micro-fiber-scanner-verb-text = Собрать пробу сканером
glove-micro-fiber-scanner-verb-message = Просканировать предмет в выбранном режиме и получить палочку с пробой.
glove-micro-fiber-scanner-mode-surface = сбор микроволокон
glove-micro-fiber-scanner-mode-own = образец предмета
glove-micro-fiber-scanner-mode-label = Режим
glove-micro-fiber-scanner-mode-verb = Выбрать режим
glove-micro-fiber-scanner-mode-tooltip = Микроволокна — образцы перчаток, одежды и снаряжения на поверхности предмета. Образец — собственный индивидуальный код выбранной пары перчаток или предмета; предмет без образца вызовет отказ.
glove-micro-fiber-scanner-mode-surface-short = Микроволокна
glove-micro-fiber-scanner-mode-own-short = Образец
glove-micro-fiber-scanner-mode-selected = Режим сканера: { $mode }.
glove-micro-fiber-scanner-switch-hint = Используйте сканер в руке, чтобы открыть круговое меню выбора режима.
glove-micro-fiber-scanner-no-own-sample = Сканирование отклонено: у предмета нет собственного индивидуального образца.
glove-micro-fiber-scanner-mode-changed = Сканирование прервано: режим сканера изменён.
glove-micro-fiber-swab-unused = На палочке нет пробы. Готовые пробы выдаёт сканер микроволокон.
glove-micro-fiber-swab-used = На палочке сохранена проба. Для её изучения вставьте её в анализатор микроволокон.
glove-micro-fiber-swab-label = проба микроволокон: { $source }
glove-micro-fiber-swab-source = Источник пробы: { $source }
glove-micro-fiber-analyzer-slot-name = Проба микроволокон
glove-micro-fiber-analyzer-interface-title = Анализатор микроволокон
glove-micro-fiber-analyzer-interface-number = № { $number }
glove-micro-fiber-analyzer-interface-number-caption = СЕРИЙНЫЙ
glove-micro-fiber-analyzer-interface-number-caption-second-line = НОМЕР
glove-micro-fiber-analyzer-interface-number-tooltip = Номер аппарата: { $number }
glove-micro-fiber-analyzer-interface-brand = NT
glove-micro-fiber-analyzer-interface-eject = Извлечь пробу
glove-micro-fiber-analyzer-interface-print = Распечатать
glove-micro-fiber-analyzer-interface-analyze = Провести анализ
glove-micro-fiber-analyzer-interface-analyzing = Выполняется анализ образца…
glove-micro-fiber-analyzer-interface-ready = Образец готов к анализу.
glove-micro-fiber-analyzer-interface-analysis-interrupted = Анализ прерван: образец извлечён.
glove-micro-fiber-analyzer-interface-description = Описание
glove-micro-fiber-analyzer-interface-description-placeholder = Введите описание образца…
glove-micro-fiber-analyzer-interface-save-description = Сохранить
glove-micro-fiber-analyzer-interface-description-value = Описание: { $description }
glove-micro-fiber-analyzer-interface-history = История
glove-micro-fiber-analyzer-interface-compare-history = Сравнение образцов
glove-micro-fiber-analyzer-interface-compare-first = Первый образец
glove-micro-fiber-analyzer-interface-compare-second = Второй образец
glove-micro-fiber-analyzer-interface-compare-run = Сравнить
glove-micro-fiber-analyzer-interface-compare-instructions = Выберите два образца из истории и нажмите «Сравнить».
glove-micro-fiber-analyzer-interface-compare-same-sample = Нужны два разных образца.
glove-micro-fiber-analyzer-interface-compare-profile-count = Совпавшие коды образцов перчаток: { $count }
glove-micro-fiber-analyzer-interface-compare-storage-micro-fiber-count = Совпавшие коды образцов хранилищ: { $count }
glove-micro-fiber-analyzer-interface-compare-no-profiles = Совпадений по кодам образцов перчаток нет.
glove-micro-fiber-analyzer-interface-compare-no-storage-micro-fibers = Совпадений по кодам образцов хранилищ нет.
glove-micro-fiber-analyzer-interface-compare-note = Совпадение кода образца указывает на конкретную пару перчаток или экземпляр хранилища, но не устанавливает его владельца.
glove-micro-fiber-analyzer-interface-compare-not-enough = Для сравнения нужны два исследованных образца в истории.
glove-micro-fiber-analyzer-interface-current-sample = Неисследованный образец
glove-micro-fiber-analyzer-interface-history-empty = История пуста.
glove-micro-fiber-analyzer-interface-history-sample = Образец { $identifier }
glove-micro-fiber-analyzer-interface-source = Исследуемый предмет: { $source }
glove-micro-fiber-analyzer-interface-source-heading = { $source }
glove-micro-fiber-analyzer-interface-analyzed-at = Время анализа: { $time }
glove-micro-fiber-analyzer-interface-printer-ready = Копировальный аппарат подключён и готов к печати.
glove-micro-fiber-analyzer-interface-printer-unavailable = Копировальный аппарат подключён, но не готов: проверьте питание, тонер и очередь печати.
glove-micro-fiber-analyzer-interface-printer-disconnected = Нет связи с копировальным аппаратом. Установите связь мультитулом.
glove-micro-fiber-analyzer-port-source-name = Отчёт анализа микроволокон
glove-micro-fiber-analyzer-port-source-description = Отправляет заключение в связанный копировальный аппарат.
glove-micro-fiber-analyzer-port-sink-name = Печать документов
glove-micro-fiber-analyzer-port-sink-description = Принимает заключения связанного анализатора для печати.
glove-micro-fiber-analyzer-report-title = Заключение экспертизы микроволокон
glove-micro-fiber-analyzer-report-header = [bold]Форма НТ-СБ-ЭСП[/bold]
    { "[head=2]Заключение экспертизы микроволокон[/head]" }

    { "[bold]Регистрационный номер:[/bold]" } { $identifier }
glove-micro-fiber-analyzer-report-source = [bold]Исследуемый предмет:[/bold] { $source }
glove-micro-fiber-analyzer-report-analyzed-at = [bold]Время завершения анализа (от начала раунда):[/bold] { $time }
glove-micro-fiber-analyzer-report-results = [head=3]Идентифицированные образцы перчаток[/head]
    { "[bold]Количество образцов перчаток:[/bold]" } { $count }
glove-micro-fiber-analyzer-report-trace = [bold]Образец № { $number }[/bold]
    { "[bold]Вид перчаток:[/bold]" } { $type }
    { "[bold]Код образца:[/bold]" } { $code }
glove-micro-fiber-analyzer-report-storage-micro-fibers = [head=3]Идентифицированные образцы хранилищ[/head]
    { "[bold]Количество образцов хранилищ:[/bold]" } { $count }
glove-micro-fiber-analyzer-report-storage-micro-fiber = [bold]Образец хранилища № { $number }[/bold]
    { "[bold]Хранилище:[/bold]" } { $type }
    { "[bold]Код образца:[/bold]" } { $code }
glove-micro-fiber-analyzer-report-description = [head=3]Описание образца[/head]
    { $description }
glove-micro-fiber-analyzer-report-footer = [italic]Документ сформирован автоматически анализатором микроволокон.[/italic]

    { "[italic]Место для подписи и печати детектива[/italic]" }
glove-micro-fiber-analyzer-interface-no-swab = Вставьте палочку с пробой.
glove-micro-fiber-analyzer-interface-unused-swab = Вставленная палочка ещё не использована.
glove-micro-fiber-analyzer-interface-traces = Образцы перчаток
glove-micro-fiber-analyzer-interface-no-traces = Образцы перчаток не обнаружены.
glove-micro-fiber-analyzer-interface-storage-micro-fibers = Образцы хранилищ
glove-micro-fiber-analyzer-interface-no-storage-micro-fibers = Образцы хранилищ не обнаружены.
glove-micro-fiber-analyzer-interface-type = Вид перчаток: { $type }
glove-micro-fiber-analyzer-interface-item-type = Хранилище: { $type }
glove-micro-fiber-analyzer-interface-code = Код образца: { $code }
glove-micro-fiber-analyzer-interface-glove-sample-number = Образец перчаток № { $number }
glove-micro-fiber-analyzer-interface-storage-sample-number = Образец хранилища № { $number }
glove-micro-fiber-analyzer-interface-empty-category = Не обнаружены
glove-micro-fiber-analyzer-interface-unknown-type = Неизвестный вид перчаток
glove-micro-fiber-analyzer-interface-unknown-item = Неизвестное хранилище
