# LotATC Squawk Classifications Compiler

In LotATC you can supply a JSON file for your use of squawk codes. 

> You can read more about [this feature here][lot-atc-json-document].

This is a very useful feature for controllers but creating the file and 
keeping it updated involves quite a lot of work, usually repetitive as 
you want to express the use of ranges of squawk codes. 

To remedy this problem and minimize the work for the squawk code layout
designer, while allowing him/her to remain flexible as requirements 
shift over time, the LotATC squawk compiler (`sqwkcomp`) is a tool that
takes a LotATC squawk code JSON template as its output and creates 
the operating LotATC JSON file you need.

## Basics

The squawk compiler takes a LotATC JSON squawk classification JSON
file but allows for special syntax for ranges, to reduce the work
involved when creating your squawk classifications. That syntax is 
very simple. Here's an example:

```json5
{
  "__comments":"THIS FILE IS AN EXAMPLE AND WILL NOT BE LOADED BECAUSE enable=false",
  "enable": true,
  "transponders" : [
    {
        "mode3": "7001",
        "comment": "VFR"
    },
    {
        "mode3": "7040-7047",
        "comment": "Aerodrome #1"
    }
  ]
}

```
In the above example there are two blocks: The first one simply expresses
squawk code 7001 adding a comment ("VFR") for it. That is fully supported
by LotATC already so the compiler will simply leave it as it finds it.

The second block is a squawk code *range* that starts with squawk code 7040
and ends with 7047. The compiler will create eight blocks from this 
range, like so:

```json5
{
  "__comments":"THIS FILE IS AN EXAMPLE AND WILL NOT BE LOADED BECAUSE enable=false",
  "enable": true,
  "transponders" : [
    {
      "mode3": "7001",
      "comment": "VFR"
    },
    {
      "mode3": "7040-7047",
      "comment": "Aerodrome #1"
    },
    {
      "mode3": "7041",
      "comment": "Aerodrome #1"
    },
    {
      "mode3": "7042",
      "comment": "Aerodrome #1"
    },
    {
      "mode3": "7043",
      "comment": "Aerodrome #1"
    },
    {
      "mode3": "7044",
      "comment": "Aerodrome #1"
    },
    {
      "mode3": "7045",
      "comment": "Aerodrome #1"
    },
    {
      "mode3": "7046",
      "comment": "Aerodrome #1"
    },
    {
      "mode3": "7047",
      "comment": "Aerodrome #1"
    }
  ]
}
```

If you needed the range to increment differently you could add an 
incrementation value to the range declaration, like in this example:

```json5
    {
        "mode3": "7040 - 7077 +10",
        "comment": "Aerodrome #1"
    }
```

That would instead have created this sequence of squawk classifications:

```json5
[
  {
    "mode3": "7040",
    "comment": "Aerodrome #1"
  },
  {
    "mode3": "7050",
    "comment": "Aerodrome #1"
  },
  {
    "mode3": "7060",
    "comment": "Aerodrome #1"
  },
    // and so on...
]
```

So, creating squawk ranges should be pretty straight forward. 
But there is a problem: The compiler just reproduced the comment 
from the template squawk range section to all the resulting blocks. 
Very likely you would have wanted the aerodromes to be numbered, like in 
this short snippet:

```json5
[  
    { 
      "mode3": "7040",
      "comment": "Aerodrome #1"
    },
    {
      "mode3": "7041",
      "comment": "Aerodrome #2"
    },
    {
      "mode3": "7042",
      "comment": "Aerodrome #3"
    },
    // and so on...
]
```

## Counters

The squawk compiler allows for defining counters within a declared 
squawk range, using this syntax: `'#('<counter id> '=' <start> ['+'<increment>])`.

Here's how you would have written the above range to achieve incrementing
the 'x' in "Aerodrome #x":

```json
    {
        "mode3": "7040-7047 #(x = 1 + 1)",
        "comment": "Aerodrome #=(x)"
    }
```

The `#(x = 1 + 1)` is interpreted by the compiler as: for this range; 
"define '`x`' as a counter, starting with 1 and incremented by 1 for 
every squawk code". So, if you wanted to start with one and increment
by 10 you would instead have written `#(x = 10 + 10)`, which would 
have named the aerodromes "Aerodrome #10", "Aerodrome #20", "Aerodrome #30"
and so on. The increment element is optional, defaulting to one (1) when
omitted.

You might need multiple counters in a range, like in this example:

```json
    {
        "mode3": "7040-7047 #(unit = 1), #(tail = 400 + 10)",
        "comment": "CAP - Devil 1-=(unit) (tail no. =(tail))"
    }
```

This would have been compiled into this output:

```json5
[
    {
        "mode3": "7040",
        "comment": "CAP - Devil 1-1 (tail no. 400)"
    },
    {
        "mode3": "7041",
        "comment": "CAP - Devil 1-2 (tail no. 410)"
    },
    {
        "mode3": "7041",
        "comment": "CAP - Devil 1-3 (tail no. 420)"
    }
    // and so on...
]
```

## Comments

The squawk compiler currently supports JSON 5 single line comments,
like in the above examples. The single line comment starts with "`//`"
and will automatically be omitted in the output (until LotATC) supports
JSON 5.

Multi-line JSON 5 comments are currently not supported.

## Advanced

Consider this squawk range again:

```json5
[  
    { 
      "mode3": "7040",
      "comment": "Aerodrome #1"
    },
    {
      "mode3": "7041",
      "comment": "Aerodrome #2"
    },
    {
      "mode3": "7042",
      "comment": "Aerodrome #3"
    },
    // and so on...
]
```

That might be great if all controllers know which aerodrome is #1, #2 and #3 
but, maybe, it would be even better if the actual aerodrome names could 
be included in the squawk comment instead. That, of course, might change
from one map to another, or even between events on the same map.

To allow for more details the squawk compiler can take a second "values" 
file as its input to resolve dynamic values. Here's how you could 
achieve this by modifying the original squawk range declaration example:

```json
{
  "__comments":"THIS FILE IS AN EXAMPLE AND WILL NOT BE LOADED BECAUSE enable=false",
  "enable": true,
  "transponders" : [
    {
        "mode3": "7001",
        "comment": "VFR"
    },
    {
        "mode3": "7040-7047 #(n = 1)",
        "comment": "$(Aerodrome #=(x))"
    }
  ]
}

```

As we have already seen this would name the aerodromes "Aerodrome #1",
"Aerodrome #2" and so on. But by surrounding the result inside a
dynamic value qualifier "$(" and ")" you indicating to the compiler
that this is a dynamic value, and that it should try and look it up
in a values file you're supplying. The values file is also a JSON file
with a single list of key/value pairs, like this:

```json5
// values-syria.json
{
  "Aerodrome #1": "Incirlik AB",
  "Aerodrome #2": "Ramat David AB",
  "Aerodrome #3": "Paphos",
  "AWACS #1": "Magic 1-1",
  "AWACS #2": "Overlord 1-1",
  "AWACS #3": "Wizard 1-1",
}
```
By supplying this file to the compiler it will automatically substitute 
all variables (whatever is found between "$(" and ")") with the corresponding
value it finds in the values file. For the above examples this is the resulting output:

```json5
[
  {
    "mode3": "7040",
    "comment": "Incirlik AB"
  },
  {
    "mode3": "7041",
    "comment": "Ramat David AB"
  },
  {
    "mode3": "7042",
    "comment": "Paphos"
  },
  {
    "mode3": "7043",
    "comment": "Aerodrome #4"
  },
  {
    "mode3": "7044",
    "comment": "Aerodrome #5"
  },
    // and so on...
]
```

The fact that the values are kept in separate files means you a free 
to create very few squawk layout templates, perhaps just one,
and then recompile it with different values files to suit your
short term needs.

Also, please note how some of the above aerodromes didn't get resolved 
to specific aerodrome names. The reason for that is that they 
("Aerodrome #4" and above) isn't available in the example values file.
When the compiler can't resolve a variable it simply retains its name,
removing the qualifier tokens (the "$(" and ")").

## Running the compiler

-- TODO --

[lot-atc-json-document]: https://www.lotatc.com/documentation/client/classification.html#transponder-format