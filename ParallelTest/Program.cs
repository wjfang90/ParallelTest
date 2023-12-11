// See https://aka.ms/new-console-template for more information
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Channels;


ParallelFor_ForEachVsFor();

ParallelForBreak();

ParallelForCancel();

ParalleForEach();

ParalleForEachIndex();

ParalleForEachLocal();

ParallelForEachPartitioner();

ParallelInvoke();

Console.WriteLine("=============================");
Console.ReadKey();


/*
注意事项：

    1、循环操作是无序的，如果需要顺序直接请使用同步执行

    2、如果涉及操作共享变量请使用线程同步锁

    3、如果是简单、量大且无等待的操作可能并不适用，同步执行可能更快

    4、注意错误的处理，如果是带数据库的操作请注意事务的使用

    5、个人测试，Parallel.ForEach 的使用效率比Parallel.For更高
*/


void ParallelFor_ForEachVsFor() {
    Console.WriteLine("==============Parallel.For vs for vs Parallel.ForEach===============");

    var nums = Enumerable.Range(11, 10).ToArray();
    var sw = Stopwatch.StartNew();

    sw.Start();
    Parallel.For(0, nums.Length, i => Console.WriteLine($"i={i},value={nums[i]},getnum={GetNum(nums[i])}"));
    sw.Stop();
    Console.WriteLine();
    Console.WriteLine($"Parallel.For time={sw.ElapsedMilliseconds}");
    Console.WriteLine();

    sw.Reset();
    sw.Start();
    for (int i = 0; i < nums.Length; i++) {
        Console.WriteLine($"i={i},value={nums[i]},getnum={GetNum(nums[i])}");
    }
    sw.Stop();

    Console.WriteLine();
    Console.WriteLine($"for time={sw.ElapsedMilliseconds}");
    Console.WriteLine();


    sw.Reset();
    sw.Start();
    Parallel.ForEach(nums, item => Console.WriteLine($"value={item},getnum={GetNum(item)}"));
    sw.Stop();
    Console.WriteLine();
    Console.WriteLine($"Parallel.ForEach time={sw.ElapsedMilliseconds}");
    Console.WriteLine();
}

void ParallelForBreak() {

    Console.WriteLine("==============Parallel.For Break===============");

    /*
     Break: 告知 Parallel 循环应在系统方便的时候尽早停止执行当前迭代之外的迭代

    Stop：告知 Parallel 循环应在系统方便的时候尽早停止执行。

    如果循环之外还有需要执行的代码则用Break,否则使用Stop
     */
    ParallelLoopResult result = Parallel.For(0, 20, (int index, ParallelLoopState pls) => {

        Console.WriteLine($"index={index},task={Task.CurrentId},thread={Thread.CurrentThread.ManagedThreadId}");

        Thread.Sleep(100);

        if (index > 10) {
            pls.Break();//提前终止
        }
    });

    //LowestBreakIteration-调用Break方法的最小任务的索引
    Console.WriteLine($"Is completed：{result.IsCompleted} LowestBreakIteration：{result.LowestBreakIteration}");

}

void ParallelForCancel() {
    Console.WriteLine("==============Parallel.For Cancel ===============");

    var cts = new CancellationTokenSource();
    cts.Token.Register(() => Console.WriteLine("###### token canceled"));

    cts.CancelAfter(3000);//3秒后取消

    var option = new ParallelOptions() { CancellationToken = cts.Token };//MaxDegreeOfParallelism-设置最大并发限制，默认-1

    try {
        ParallelLoopResult result = Parallel.For(0, 1000, option, (int i, ParallelLoopState pls) => {

            if (option.CancellationToken.IsCancellationRequested) {
                Console.WriteLine("===== request canceled");
                pls.Stop();//Stop：告知 Parallel 循环应在系统方便的时候尽早停止执行。
            }

            Console.WriteLine($"index={i} start");
            Thread.Sleep(500);
            Console.WriteLine($"index={i} end");
        });

    }
    catch (OperationCanceledException ex) {
        Console.WriteLine($"****** OperationCanceledException {ex.Message}");
    }
    catch (Exception ex) {
        Console.WriteLine(ex.Message);
    }
}

void ParalleForEach() {
    Console.WriteLine("==============Parallel.ForEach===============");

    var nums = Enumerable.Range(11, 10).ToArray();
    Parallel.ForEach(nums, item => Console.WriteLine($"value={item}"));
}

void ParalleForEachIndex() {

    Console.WriteLine("==============Parallel.ForEach  index===============");
    var nums = Enumerable.Range(11, 10).ToArray();
    Parallel.ForEach(nums, (item, pls, index) => {
        Console.WriteLine($"value={item},index={index}");
    });
}

void ParalleForEachLocal() {
    Console.WriteLine("==============Parallel.ForEach  local===============");
    ConcurrentDictionary<string, int> dict = new ConcurrentDictionary<string, int>();
    var colors = new string[] { "red", "green", "blue", "yellow" };

    ParallelLoopResult result = Parallel.ForEach<string, KeyValuePair<string, int>>(
        colors,
        () => default, //local初始化函数

        (item, pls, index, localItem) => {
            //localItem 初始化参数
            localItem = new KeyValuePair<string, int>(item, item.Length);

            Console.WriteLine($"value={item},index={index},length={item.Length}");

            return localItem;
        },
    localFinally => {//local 最终处理函数
        dict.TryAdd(localFinally.Key, localFinally.Value);
    });

    Console.WriteLine();
    foreach (var item in dict.Keys) {
        Console.WriteLine($"color={item},length={dict[item]}");
    }
}

void ParallelForEachPartitioner() {
    Console.WriteLine("==============Parallel.ForEach Partitioner===============");

    var nums = Enumerable.Range(11, 10).ToArray();
    var numsOrderablePartitioner = Partitioner.Create(nums, true);//OrderablePartitioner 是一个用于在并行循环中分割数据的类，它可以将数据划分为可排序的分区，并支持动态负载平衡
    Parallel.ForEach(numsOrderablePartitioner, item => Console.WriteLine($"value={item}"));
}

void ParallelInvoke() {
    Console.WriteLine("==============Parallel.Invoke ===============");
    /*
     1、如果操作小于10个，使用Task.Factory.StartNew 或者Task.Run 效率更高

     2、适合用于执行大量操作且无需返回结果的场景
     */
    Parallel.Invoke(Foo, Bar);
}

int GetNum(int i) {
    Thread.Sleep(100);
    return i * i;
}

void Foo() {
    Console.WriteLine("Foo");
}

void Bar() {
    Console.WriteLine("Bar");
}
