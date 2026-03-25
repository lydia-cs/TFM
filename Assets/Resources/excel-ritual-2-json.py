import pandas as pd
import json
import os

FILE_PATH = "./Ritual.xlsx"
xls = pd.ExcelFile(FILE_PATH)

# Dictionary to store data from all sheets
data_dict = {}
anim_data = {}  # To store AnimSyncs and AnimEvents

no_split_columns = ["dialogue", "extendeddescription", "description", "buttontext"] # Columns that should never be split into lists
force_list_columns = ["Scale", "Rotation", "Location"] # Columns that should always be lists

for sheet_name in xls.sheet_names:
    df = pd.read_excel(xls, sheet_name=sheet_name, dtype=str)  # Read each sheet
    
    sheet_data = []  # List to store row dictionaries for this sheet

    for i in range(len(df)):
        row_dict = {}  # Dictionary for each row
        
        for column in df.columns:
            value = df[column][i]

            # Skip NaN values (don't add them to JSON)
            if pd.isna(value) or value == "nan":
                continue 

            # Automatically split values with commas
            if isinstance(value, str) and column.lower() not in no_split_columns:
                if "," in value:
                    parts = value.split(",")  # Split by comma
                
                    # Try to convert all parts to float if possible
                    try:
                        parts = [float(x.strip()) for x in parts]
                    except ValueError:
                        parts = [x.strip() for x in parts]  # Keep as strings if conversion fails
                
                    value = parts  # Otherwise, store as a list

                # Ensure even single values in forced list columns are lists
                elif column in force_list_columns:
                    value = [value.strip()]  # Convert single value to list

            row_dict[column] = value  # Store processed value

        sheet_data.append(row_dict)

    # Handle AnimSyncs and AnimEvents specially
    if sheet_name in ["AnimSyncs", "AnimEvents"]:
        anim_data[sheet_name] = sheet_data
    elif sheet_name in ["Animations"]:
        anim_data[sheet_name] = sheet_data
        data_dict[sheet_name] = sheet_data
    else:
        data_dict[sheet_name] = sheet_data  # Store sheet data in main dictionary

# Save main data to ritual.json
with open("ritual.json", "w", encoding="utf8") as f:
    json.dump(data_dict, f, ensure_ascii=False, indent=4)

# Save AnimData (AnimSyncs + AnimEvents)
if anim_data:
    with open("animData.json", "w", encoding="utf8") as f:
        json.dump(anim_data, f, ensure_ascii=False, indent=4)

print("Data saved to ritual.json and animData.json")