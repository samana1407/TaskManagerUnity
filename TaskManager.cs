using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Samana.Tasks
{
    public class TaskManager : MonoBehaviour
    {
        public static TaskManager instance;
        public bool pause;

        List<TaskQueue> _queues;
        List<TaskQueue> _queuesForRemove;
        bool _isUpdate;

        private void Awake()
        {
            instance = this;
            _queues = new List<TaskQueue>();
            _queuesForRemove = new List<TaskQueue>();
        }

        /// <summary>
        /// Возвращает ссылку на заданную очередь заданий, либо создаёт новую, если такой очереди нет.
        /// </summary>
        /// <param name="queueName">Имя для очереди заданий.</param>
        /// <returns>Ссылка на очередь заданий.</returns>
        public TaskQueue getOrCreateQueue(string queueName)
        {
            for (int i = 0; i < _queues.Count; i++)
            {
                if (_queues[i].name == queueName) return _queues[i];
            }
            TaskQueue newQueue = new TaskQueue(queueName);
            _queues.Add(newQueue);

            return newQueue;
        }

        /// <summary>
        /// Добавляет очередь в менеджер, если она ещё не добавлена и имеет уникальное имя.
        /// </summary>
        /// <param name="queue"></param>
        /// <returns></returns>
        public TaskQueue addQueue(TaskQueue queue)
        {
            for (int i = 0; i < _queues.Count; i++)
            {
                if (_queues[i] == queue || _queues[i].name == queue.name)
                {
                    Debug.LogWarningFormat("[TaskManager][addQueue] Очередь с именем '{0}' уже добавлена. Отмена.", queue.name);
                    return null;
                }
            }
            _queues.Add(queue);
            return queue;
        }

        /// <summary>
        /// Удаляет очередь заданий.
        /// </summary>
        /// <param name="queueName">Имя очереди, которую надо удалить.</param>
        public void removeQueue(string queueName)
        {
            for (int i = 0; i < _queues.Count; i++)
            {
                if (_queues[i].name == queueName)
                {
                    if (pause || !_isUpdate) _queues.RemoveAt(i);
                    else
                    {
                        // нужно для того, если поступила команда на удаление очереди во время её выполнения,
                        // такая очередь удалится только после своей отработки.
                        if (!_queuesForRemove.Contains(_queues[i])) _queuesForRemove.Add(_queues[i]);
                    }

                    break;
                }
            }
        }

        /// <summary>
        /// Удаляет все очереди, в которых нет заданий.
        /// </summary>
        public void removeAllEmptyQueues()
        {
            for (int i = _queues.Count - 1; i >= 0; i--)
            {
                if (_queues[i].isEmpty) _queues.RemoveAt(i);
            }
        }

        private void Update()
        {
            if (pause) return;
            _isUpdate = true;

            float deltaTime = Time.deltaTime;

            TaskQueue currentQueue;
            for (int i = 0; i < _queues.Count; i++)
            {
                if (_queuesForRemove.Contains(_queues[i])) continue; //если очередь в списке "надо удалить", то проходим мимо

                currentQueue = _queues[i];
                currentQueue.Invoke(deltaTime);
            }

            if (_queuesForRemove.Count != 0) // удаляем все очереди из списка "надо удалить"
            {
                for (int i = 0; i < _queuesForRemove.Count; i++) _queues.Remove(_queuesForRemove[i]);
                _queuesForRemove.Clear();
            }

            _isUpdate = false;
        }
    }

    public class TaskQueue
    {
        public string name { get; private set; }
        List<Task> _tasks;
        public Task[] tasks { get { return _tasks.ToArray(); } }
        public bool isEmpty { get { return _tasks.Count == 0; } }
        public bool pause;

        public TaskQueue(string queueName)
        {
            name = queueName;
            _tasks = new List<Task>();
        }
        /// <summary>
        /// Добавляет задание в самый конец очереди.
        /// </summary>
        /// <param name="task">Текущее задание.</param>
        /// <returns>Возвращает ссылку на очередь.</returns>
        public TaskQueue addTask(Task task)
        {
            _tasks.Add(task);
            return this;
        }

        /// <summary>
        /// Добавляет массив заданий в самый конец очереди.
        /// </summary>
        /// <param name="task">Массив заданий.</param>
        /// <returns>Возвращает ссылку на очередь.</returns>
        public TaskQueue addTask(Task[] tasks)
        {
            _tasks.AddRange(tasks);
            return this;
        }

        /// <summary>
        /// Добавляет новое задание после текущего.
        /// </summary>
        /// <param name="task">Новое задание.</param>
        /// <returns>Возвращает ссылку на очередь.</returns>
        public TaskQueue insertAfterCurrent(Task task)
        {
            if (_tasks.Count != 0) _tasks.Insert(1, task);
            else addTask(task);

            return this;
        }

        /// <summary>
        /// Добавляет массив новых заданий после текущего.
        /// </summary>
        /// <param name="task">Массив из заданий.</param>
        /// <returns>Возвращает ссылку на очередь.</returns>
        public TaskQueue insertAfterCurrent(Task[] tasks)
        {
            if (_tasks.Count != 0) _tasks.InsertRange(1, tasks);
            else addTask(tasks);
            return this;
        }

        /// <summary>
        /// Создаёт клон текущего задания с таймерами по-умолчанию и вставляет клон после текущего задания.
        /// </summary>
        /// <returns>Возвращает ссылку на очередь.</returns>
        public TaskQueue repeatCurrent()
        {
            Task currentTask = getCurrentTask();
            if (currentTask != null) insertAfterCurrent(currentTask.cloneWithResetTimers());

            return this;
        }

        /// <summary>
        /// Возвращает текущее задание если оно есть, либо null.
        /// </summary>
        /// <returns></returns>
        public Task getCurrentTask()
        {
            if (_tasks.Count != 0) return _tasks[0];
            return null;
        }

        /// <summary>
        /// Удаляет из очереди все задания после текущего.
        /// </summary>
        /// <returns></returns>
        public TaskQueue removeTasksAfterCurrent()
        {
            if (_tasks.Count > 1) _tasks.RemoveRange(1, _tasks.Count - 1);
            return this;
        }

        /// <summary>
        ///  Очищает очередь.
        /// </summary>
        /// <returns></returns>
        public TaskQueue clear()
        {
            _tasks.Clear();
            return this;
        }

        /// <summary>
        /// Вызывает текущее задание.
        /// </summary>
        /// <param name="deltaTime">Время для работы таймеров заданий.</param>
        public void Invoke(float deltaTime)
        {
            if (pause) return;
            if (_tasks.Count != 0)
            {
                _tasks[0].invoke(deltaTime);

                if (isEmpty) return; // возможно таск выше вызвал очистку очереди
                if (_tasks[0].isDone) _tasks.RemoveAt(0);
            }
        }

    }

    public class Task
    {
        private Func<bool> _taskFunc;
        private Action _taskAction;

        public float delay = 0;
        private float _delay;

        public float breakTime;
        private float _breakTime;

        public int repeat = 0;
        private int _repeat;

        public Action endCallback;
        public bool isDone { get; private set; }

        public Task(Func<bool> task) { this._taskFunc = task; }
        public Task(Action task) { this._taskAction = task; }

        public void invoke(float deltaTime)
        {
            _delay += deltaTime;
            if (_delay >= delay)
            {
                if (_taskFunc != null)
                {
                    isDone = _taskFunc();

                    if (breakTime != 0)
                    {
                        _breakTime += deltaTime;
                        if (_breakTime >= breakTime) isDone = true;
                    }
                }
                else if (_taskAction != null)
                {
                    _taskAction();
                    if (breakTime != 0)
                    {
                        _breakTime += deltaTime;
                        isDone = _breakTime >= breakTime;
                    }
                    else isDone = true;
                }

                if (isDone && endCallback != null) endCallback();

                if (isDone && repeat > 0 && _repeat < repeat - 1)
                {
                    _repeat++;
                    resetTimers();
                }

            }
        }

        private void resetTimers()
        {
            _delay = 0;
            _breakTime = 0;
            isDone = false;
        }

        /// <summary>
        /// Создаёт и возвращает клон этого задания со сброщенными приватными таймерами.
        /// </summary>
        /// <returns></returns>
        public Task cloneWithResetTimers()
        {
            if (_taskFunc != null)
                return new Task(_taskFunc) { delay = this.delay, breakTime = this.breakTime, endCallback = this.endCallback, repeat = repeat };
            else
                return new Task(_taskAction) { delay = this.delay, breakTime = this.breakTime, endCallback = this.endCallback, repeat = repeat };
        }
    }

}





