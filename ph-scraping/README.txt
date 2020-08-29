This program has the sole purpose of scraping Project Hospital game files for information pertatining to diagnoses. It will generate an output file
comprised of every diagnosis in the game.

In the generateOutputFile() method, there are three boolean values which dictate which output format will be used. These values have to be changed manually,
as there isn't a prompt at the commandline for them.

In order to update the output file successfully:

1. Ensure all relevant information is pulled from game files. Note that with new updates and DLC packs, files may be thrown around senselessly in various folders.
    - You will need files pertaining to the diagnoses, symptoms, examinations, and treatments.
    - Most segfault errors are the cause of missing file entries.
2. Run the program. This will take approximately a second.
3. Check \Output\Merged for the comprehensive-diagnoses-list.txt This contains all diagnoses in the game formatted in accordance with the boolean value selected.
4. Copy comprehensive-diagnoses-list.txt to the desktop.
5a. If using the Steam format:
    - Copy and paste individual chunks.
    - Cry at how tedious it is.
5b. If using the Plain format:
    - You're finished!
5c. If using the Drive format:
    - Open the Command Prompt.
    - Type 'cd desktop'
    - Type 'pandoc comprehensive-diagnoses-list.txt -o comprehensive-diagnoses-list.docx'
    - This will take a few seconds to process.
    - Open the generated Word document and modify it to match the Google Drive format.
        - Header 1 (Dept Name): Caliibri, 22pt, bold, underline
        - Header 2 (Diagnosis Name): Calibri, 18pt, bold, underline
        - Header 3 (Symptoms Subtitle): Caliibri, 14pt, underline
        - Body: Caliibri, 12pt