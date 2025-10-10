using System;
using TestComponent;
using TestComponentCSharp;

CustomDisposableTest customDisposableTest = new();
customDisposableTest.Dispose();

Composable composable = new();
_ = composable.Value;
composable.Value = 5;
_ = composable.Value;

_ = Composable.ExpectComposable(composable);
_ = Composable.ExpectRequiredOne(composable);

Composable composable2 = new(42);
_ = composable2.Value;

_ = Composable.ExpectComposable(composable2);
_ = Composable.ExpectRequiredOne(composable2);

_ = ComImports.NumObjects;
_ = ComImports.MakeObject();

TestComposable testComposable = new();

_ = testComposable.Value;

_ = Composable.ExpectComposable(testComposable);
_ = Composable.ExpectRequiredOne(testComposable);

sealed class TestComposable : Composable
{
}

/*
// new RCW / Factory activation
var instance = new Class();

var expectedEnum = EnumValue.Two;
instance.EnumProperty = expectedEnum;

// Custom type marshaling
var expectedUri = new Uri("http://expected");
instance.UriProperty = expectedUri;

var instance2 = new Class(32);

return instance.EnumProperty == expectedEnum && 
       instance.UriProperty == expectedUri &&
       instance2.IntProperty == 32 ? 100 : 101;
*/