# Api Client Generator

This can generate an api client code using C# reflection

#### Why generate using reflection instead of swagger
- Consistency with the api codebase
- We can have 1 api client file per api and avoid clutter and potential collisions when other apis are present
- It's .Net Core _(same framework as the api code)_, hence, more accessible, maintainable, and less hassle to setup than swagger codegen especially on customizability

#### Why use swagger generator instead
- It might not be in sync with open api spec
- Breaking changes from how .net generates api might break the generated api client as well
- You wish to generate multiple apis that were built with different platforms _(e.g. .net core, expressjs, php, python, etc.)_