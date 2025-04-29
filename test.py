from math import log2, ceil

half_parts = 16
for main in range(1, ceil(log2(half_parts)) + 1):
    for i in range(main, 0, -1):
        swap_num = i
        for x in range(8):
            log_parts = int(log2(half_parts))
            floored = (x << swap_num) >> log_parts
            modded = (x << swap_num) - (floored << log_parts)
            first = modded + floored
            last = first + (1 << (swap_num - 1))
            print(first, last, (modded >> (main)) & 1)
        print()