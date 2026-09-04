"""Experimental attention adaptation; not imported by the application."""


def expand_grouped_kv(query, key, value):
    return (
        key.repeat_interleave(query.shape[-3] // key.shape[-3], dim=-3),
        value.repeat_interleave(query.shape[-3] // value.shape[-3], dim=-3),
    )
