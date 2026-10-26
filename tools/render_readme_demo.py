"""Render a README animation from verified converter output and the existing app icon.

Optional developer dependency: Pillow. This is an illustrated explanation, not
a screen recording or benchmark. No live application, input, or clipboard is used.
"""

from pathlib import Path
import argparse
import json
import os
import random


def main(argv: list[str] | None = None) -> None:
    """Render the approved sequence with local state and deterministic typing timing."""
    root = Path(__file__).resolve().parent.parent
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--words", type=Path, required=True)
    args = parser.parse_args(argv)
    payload = json.loads(args.words.read_text(encoding="utf-8-sig"))
    if (
        payload["krishna"] != "Kṛṣṇa"
        or payload["ramaChoices"] != ["Rāma", "Ramā", "Rāmā", "Rama"]
        or payload["sp"] != "Śrīla Prabhupāda"
        or payload["ki"] != "kī"
    ):
        raise ValueError("Demo words must match the verified conversion sequence")
    from PIL import Image, ImageDraw, ImageFont

    output = root / "docs/images"
    output.mkdir(parents=True, exist_ok=True)

    # Reuse the approved white transparent icon on a dark tile for both GitHub themes.
    # The app's tray icon asset is untouched.
    icon = (
        Image.open(root / "assets/keyjaya.ico").ico.getimage((256, 256)).convert("RGBA")
    )
    logo = Image.new("RGBA", (256, 256), (0, 0, 0, 0))
    ImageDraw.Draw(logo).rounded_rectangle((0, 0, 255, 255), radius=40, fill="#111827")
    logo.alpha_composite(icon.resize((224, 224), Image.Resampling.LANCZOS), (16, 16))
    logo.save(output / "keyjaya-logo.png", optimize=True)

    fonts = Path(os.environ.get("WINDIR", "C:/Windows")) / "Fonts"
    font = ImageFont.truetype(str(fonts / "cambria.ttc"), 50)
    finished = [
        "Hare " + payload["krishna"],
        "Hare " + payload["ramaChoices"][0],
        payload["sp"] + " " + payload["ki"] + " jaya!",
    ]
    width = int(max(font.getlength(line) for line in finished)) + 52
    height, gap = 220, 64
    typed = [""]
    frames, durations, trace = [], [], []
    typing_random = random.Random(194)

    def frame(caret=True, pos=None):
        """Draw the current lines and optional caret without advancing the sequence."""
        image = Image.new("L", (width, height), 255)
        draw = ImageDraw.Draw(image)
        for row, line in enumerate(typed):
            assert font.getlength(line) <= width - 48, line
            draw.text((24, 6 + row * gap), line, font=font, fill=0)
        if caret:
            row, offset = pos if pos is not None else (len(typed) - 1, len(typed[-1]))
            x = 25 + font.getlength(typed[row][:offset])
            y = 6 + row * gap + int(font.size * 0.26)
            draw.line((x, y, x, y + int(font.size * 0.88)), fill=0, width=2)
        return image

    def append(milliseconds, caret=True, pos=None):
        """Record one image, its delay, and a matching diagnostic timeline entry."""
        frames.append(frame(caret, pos))
        durations.append(milliseconds)
        trace.append(
            {
                "text": "\n".join(typed),
                "duration": milliseconds,
                "caret": (pos or (len(typed) - 1, len(typed[-1]))) if caret else None,
            }
        )

    def type_text(text):
        """Append characters with reproducible, deliberately varied typing delays."""
        for char in text:
            typed[-1] += char
            append(typing_random.choice([120, 140, 150, 170, 190, 210]))

    def backspace(count):
        """Illustrate an intentional correction; this never generates keyboard input."""
        for _ in range(count):
            assert typed[-1]
            typed[-1] = typed[-1][:-1]
            append(typing_random.choice([100, 120, 140]))

    def newline():
        append(250)
        typed.append("")
        append(250)

    # One continuous scene: earlier lines stay visible while typing continues.
    append(450)
    type_text("Hare Krishan")
    append(350)
    backspace(2)
    type_text("na")
    assert typed[-1] == "Hare Krishna"
    append(350)
    typed[-1] = "Hare " + payload["krishna"]
    append(700)

    newline()
    type_text("Hare Rama")
    append(450)
    # These are the actual consecutive Ctrl choices, including the plain spelling
    # and the wrap back to the first spelling. No runtime or dictionary is changed.
    for choice in payload["ramaChoices"] + [payload["ramaChoices"][0]]:
        typed[-1] = "Hare " + choice
        append(550)
        append(350, False)

    newline()
    type_text("SP")
    # A Ctrl tap expands SP before the remainder of the line is typed.
    append(700)
    typed[-1] = payload["sp"]
    append(220)
    type_text(" ki jaya!")
    assert typed[-1] == payload["sp"] + " ki jaya!"
    append(500)

    # Arrow back to just after the i, then use backtick for i → ī.
    ki_end = len(payload["sp"]) + 3
    for offset in range(len(typed[-1]) - 1, ki_end - 1, -1):
        append(110, pos=(2, offset))
    append(350, pos=(2, ki_end))
    typed[-1] = typed[-1][: ki_end - 2] + payload["ki"] + typed[-1][ki_end:]
    append(700, pos=(2, ki_end))
    assert typed == finished
    append(450)
    append(450, False)
    append(450)
    append(1900, False)

    frame(False).save(output / "keyjaya-demo-still.png", optimize=True)
    # Common grayscale colors keep the anti-aliased lettering smooth.
    indexed = [image.convert("P") for image in frames]
    indexed[0].save(
        output / "keyjaya-demo.gif",
        save_all=True,
        append_images=indexed[1:],
        duration=durations,
        loop=0,
        disposal=1,
        optimize=True,
        comment=b"Illustration using KeyJaya converter output. Timing is illustrative, not a benchmark.",
    )
    trace_path = root / "out/readme-demo/timeline.json"
    trace_path.write_text(
        json.dumps(trace, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
    )
    with Image.open(output / "keyjaya-demo.gif") as actual:
        print(
            f"Rendered one-scene typing GIF: {actual.n_frames} frames, {sum(durations)} ms, {width}x{height}"
        )


if __name__ == "__main__":
    main()
