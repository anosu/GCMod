using System;
using BepInEx.Configuration;

namespace GCMod;

// 正整数没有合理的固定上限，保留文本输入而不生成跨度过大的滑块。
internal sealed class AcceptablePositiveInt : AcceptableValueBase
{
    internal AcceptablePositiveInt()
        : base(typeof(int)) { }

    public override object Clamp(object value) => Math.Max(1, (int)value);

    public override bool IsValid(object value) => value is int number && number > 0;

    public override string ToDescriptionString() => "# Acceptable values: positive integers";
}
