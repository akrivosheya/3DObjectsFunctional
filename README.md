# 3DObjectsFunctional
My super puper functional

Данный функционал является результатом моей работы в качестве пракиканта в Институте нефтегазовой геологии и геофизики им. А.А. Трофимука Сибирского отделения Российской академии наук (ИНГГ СО РАН).

Основной задачей было ввести возможность при нанесении удара молотком по объекту соответствующим образом разделить его на 2 половинки.
На месте среза накладывается текстура объекта в разрезе.
Основной алгоритм был взят из лекции: https://www.gdcvault.com/play/1026882/How-to-Dissect-an-Exploding

Во время работы компонента используются потоки для распараллеливания перебора множества вершин объектов, а также для совершения всего процесса разрезания на фоне.
Из-за этого объект раскалывается на части только спустя небольшое время после удара, но при этом само приложение не зависает. Число потоков может регулироваться из редактора.
Разрезать объекты можно по несколько раз, пока не будет достигнут указанный в редакторе лимит.