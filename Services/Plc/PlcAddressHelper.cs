using System;

public enum PlcAddressType
{
    M, // 内部继电器，线圈
    D, // 数据寄存器，保持寄存器
}

public static class PlcAddressHelper
{
    public static PlcAddressType ParseType(string address, out ushort addr)
    {
        if (string.IsNullOrWhiteSpace(address))
            throw new ArgumentException("地址不能为空");

        address = address.Trim().ToUpper();
        char type = address[0];
        if (!ushort.TryParse(address.Substring(1), out addr))
            throw new ArgumentException("地址数字部分错误");
        switch (type)
        {
            case 'M':
                return PlcAddressType.M;
            case 'D':
                return PlcAddressType.D;
            default:
                throw new NotSupportedException($"不支持的地址类型: {type}");
        }
    }
}
