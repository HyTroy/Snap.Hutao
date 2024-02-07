﻿// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Win32.System.Console;

[SuppressMessage("", "CA1069")]
[Flags]
internal enum CONSOLE_MODE : uint
{
    ENABLE_PROCESSED_INPUT = 0x001U,
    ENABLE_LINE_INPUT = 0x002U,
    ENABLE_ECHO_INPUT = 0x004U,
    ENABLE_WINDOW_INPUT = 0x008U,
    ENABLE_MOUSE_INPUT = 0x010U,
    ENABLE_INSERT_MODE = 0x020U,
    ENABLE_QUICK_EDIT_MODE = 0x040U,
    ENABLE_EXTENDED_FLAGS = 0x080U,
    ENABLE_AUTO_POSITION = 0x100U,
    ENABLE_VIRTUAL_TERMINAL_INPUT = 0x200U,
    ENABLE_PROCESSED_OUTPUT = 0x01U,
    ENABLE_WRAP_AT_EOL_OUTPUT = 0x02U,
    ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x04U,
    DISABLE_NEWLINE_AUTO_RETURN = 0x08U,
    ENABLE_LVB_GRID_WORLDWIDE = 0x10U,
}