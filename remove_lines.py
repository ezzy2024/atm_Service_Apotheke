import sys

filepath = r"c:\Users\ezzel\Projects\ServiceApotheke_MegaProject\ServiceApotheke.API\Program.cs"
with open(filepath, "r", encoding="utf-8") as f:
    lines = f.readlines()

new_lines = lines[:280] + lines[704:]

with open(filepath, "w", encoding="utf-8") as f:
    f.writelines(new_lines)

print("Removed lines successfully.")
