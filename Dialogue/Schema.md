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

### Tests

These are all theoretical right now, but I think they'd be good to implement and have run
- Tokenize each string
  - **Verify** that there are as many opening brackets as there are closing brackets
  - **Verify** that brackets are closed in the order they are opened
- Run through the calculations of printing the string if it were entirely visible
  - **Verify** that the string doesn't overflow the box
- 

### To-Do