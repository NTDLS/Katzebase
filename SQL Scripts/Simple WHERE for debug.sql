 (
        [locationid] = [6]
        AND [shelf] != [r]
        AND [quantity] = [299]
)
OR  (
        [locationid] = [6]
        AND [shelf] != [m]
        AND [quantity] = [299]
        OR [productid] = [366]
)
AND  (
        [bin] = [8]
        OR [bin] = [11]
)
