using TestComponentCSharp;

var instance = new Class();
var blittable = instance.GetBlittableStructVector();

int sum = 0;
int expected_sum = 0;
for (int i = 0; i < blittable.Count; i++)
{
    sum += blittable[i].blittable.i32;
    expected_sum += i;
}

var nonblittable = instance.GetNonBlittableStructVector();
for (int i = 0; i < nonblittable.Count; i++)
{
    sum += nonblittable[i].blittable.i32;
    expected_sum += i;
}

return sum == expected_sum ? 100 : 101;

