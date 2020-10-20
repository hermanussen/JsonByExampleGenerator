![dotnet](https://github.com/hermanussen/CompileTimeMethodExecutionGenerator/workflows/dotnet/badge.svg)

# Compile Time Method Execution Generator

A ".NET 5 preview" source generator proof of concept that allows executing a method during compilation, so that it can be really fast during runtime.

## What does it do?

[This blogpost](https://devblogs.microsoft.com/dotnet/introducing-c-source-generators/) describes how C# source generators work. In short, C# source generators allow code to be generated and added to the compilation while the compilation is running.

This particular generator looks for a method that is decorated with the `[CompileTimeExecutor]` attribute, and then adds the same method with a "CompileTime" postfix in its name to the same class. This method returns the result immediately without having to execute the logic.

This is possible, because the source generator compiles and executes the original method and then has the new "CompileTime" version of the method return the result immediately.

## But why?

I'm not sure if there are that many real world use cases, but please let me know if you have one ([Send me a Tweet](https://twitter.com/knifecore/)).

I just thought it would be fun. And potentially, it could be useful if you have a method that:
- Always returns the same result
- The implementation is slow
- You may want to change the implementation of the method during development
- You don't mind if this slightly slows down your project's compilation

It may actually be a really bad idea to use this, once .NET 5 is released. I could imagine that visual studio will be very slow because of this (because the live compilation will also be very slow).

## How does it work?

Well, take a look for yourself! The "CompileTimeMethodExecutionGenerator.Example" shows how you could use the generator. A method that calculates pi in 20000 digits is decorated with the "CompileTime" attribute. The program benchmarks running the method during runtime as well as compile time.

If you want to understand the inner workings, then take a look at the "CompileTimeMethodExecutionGenerator.Generator" project, which contains the actual generator.

## How can I see the result?

Just take a look at the output of [the latest run here](https://github.com/hermanussen/CompileTimeMethodExecutionGenerator/actions?query=workflow%3Adotnet). In the build log, expand the "Run" section and you'll find something like this:
```
Pi calculated with 20000 digits
Execution took 26482.0309ms
Pi calculated with 20000 digits (but performed calculation during compilation)
Execution took 0.2029ms
```

## How can I run it myself?

I've rolled the compilation and running of the example in a Docker that is included here. The easiest way to run it is by running `docker-compose up --build` and looking at the output. Example output:
```
Attaching to compiletimemethodexecutiongenerator_cg_1
cg_1  | Pi calculated with 20000 digits
cg_1  | Execution took 22455.9488ms
cg_1  | Pi calculated with 20000 digits (but performed calculation during compilation)
cg_1  | Execution took 0.1939ms
compiletimemethodexecutiongenerator_cg_1 exited with code 0
```

As you can see, there are significant performance gains possible. Even though this is a very contrived example, obviously.

## Is there a future for this?

I don't know. It's just something that I thought was interesting. There are many limitations at this point, that could be addressed:
- I've only tested with `string` and `int` as the return type. Potentially, the generator could support any serializable type.
- The methods can not have any parameters at this time. The parameter values should be known at compile time for this to work. Maybe adding the values to the attribute or looking at the syntax trees could be used to determine these values.
- There's probably a million ways in which using this could go wrong at this point. I should add some unit tests and handle many of the edge cases.
- ... send me your ideas and opinions ([Send me a Tweet](https://twitter.com/knifecore/))!

I may invest some time in this if there are more people interested.
