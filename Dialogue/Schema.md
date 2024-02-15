# Textbox Documentation

## File Information

### Metadata

- Filename: `<case_insensitive_dialogue_id>.yaml`
    - This will be how the `Textbox` Godot node will know what dialogue to load/display
    - It can be provided by setting a property of the node in the editor, or by passing it as a constructor param in code

### File Structure

```yaml
---
# Hashtag starts a comment. 
# The --- at the top of a YAML file is standard. Technically optional for the component, but you should include it
description: Entirely arbitrary, optional, only for human reference, overall textbox description.
# An element without a same-line value will start a parent value. An indented hyphen will indicate a list item
conversation: 
    - name: Dalton
      dialogue: 
        - This is the first line of dialogue. It will be read in and processed.
        - For every additional line of dialogue, you'll put it on a new line with a hyphen
        - You need to make sure that each of these lines fits within a single textbox. I'll provide specifics when I have them
    - name: Patrick
      dialogue:
        - As you can see, the strings are entirely unquoted. You can just type them
    - name: David
      dialogue:
        - Each list item can have multiple elements. This is exemplified here.
        - Note that there is only a hyphen before "name" and not before "dialogue". This indicates they're part of the same list item
```

## In-Engine Information

### To-Do