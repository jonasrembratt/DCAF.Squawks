# LotATC Squawk Classifications Compiler

In LotATC you can supply a JSON file for your use of squawk codes. 

> You can read more about [this feature here][lot-atc-json-document].

This is a very useful feature for controllers but creating the file and 
keeping it updated involves quite a lot of work (usually repetitive) as 
you want to express the use of ranges of squawk codes. 

To remedy this problem and minimize the work for the squawk code layout
designer, while allowing him/her to remain flexible as requirements 
shift over time, the LotATC squawk classifications compiler (`lascc`) 
is a tool that accepts a LotATC squawk code JSON template file as its 
input, with an optional data file, and compiles the operating LotATC JSON 
file you need, ready to be uploaded to your LotATC server.

## Basics

The squawk compiler expects a LotATC JSON squawk classification JSON
file as its input but allows for special syntax for squawk ranges and variables, 
to reduce the work involved when creating your squawk classifications. 
The syntax is very simple. Here's an example:

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
      "mode3": "7040 - 7047",      // <-- Squawk range
      "comment": "$(Aerodrome #0)" // <-- variable
    }
  ]
}

```
In the above example there are two squawk classifications: 
The first one simply expresses squawk code 7001 adding a comment ("VFR") for it. 
This is fully supported by LotATC already so the compiler will simply leave it 
as it finds it.

The second block is a squawk code *range* that starts with squawk code 7040
and ends with 7047. The comment value specifies a *variable*, 
that can be resolved if the compiler have access to a "data" file that declares
a values for that variable. 

> For now we'll just assume there's no data for the variable. 
> We will talk more about variables later, in 
> [Template file with variables](#template-file-with-variables) section.

The compiler will create eight blocks from this 
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
      "comment": "Aerodrome #0"
    },
    {
      "mode3": "7041",
      "comment": "Aerodrome #0"
    },
    {
      "mode3": "7042",
      "comment": "Aerodrome #0"
    },
    {
      "mode3": "7043",
      "comment": "Aerodrome #0"
    },
    {
      "mode3": "7044",
      "comment": "Aerodrome #0"
    },
    {
      "mode3": "7045",
      "comment": "Aerodrome #0"
    },
    {
      "mode3": "7046",
      "comment": "Aerodrome #0"
    },
    {
      "mode3": "7047",
      "comment": "Aerodrome #0"
    }
  ]
}
```

If you needed the squawk range to increment differently you could add an 
incrementation value to the range declaration, like in this example:

```json5
  {
    "mode3": "7040 - 7077 +10",
    "comment": "$(Aerodrome #0)"
  }
```

That would instead have created this sequence of squawk classifications:

```json5
[
  {
    "mode3": "7040",
    "comment": "Aerodrome #0"
  },
  {
    "mode3": "7050",
    "comment": "Aerodrome #0"
  },
  {
    "mode3": "7060",
    "comment": "Aerodrome #0"
  }, 
  // and so on...
]
```

So, as you can see, declaring squawk ranges is pretty straight forward. 
But how about that "Aerodrome #0" value? The compiler just reproduced 
the comment from the template squawk range section to all the resulting blocks. 
But maybe you wanted them to also be incremented, like in 
this short snippet:

```json5
[
  { 
    "mode3": "7040",
    "comment": "Aerodrome #0"
  },
  {
    "mode3": "7041", 
    "comment": "Aerodrome #1"
  },
  {
    "mode3": "7042",
    "comment": "Aerodrome #2"
  },
  // and so on...
]
```

## Counters

The squawk compiler also lets tou define counters for a declared 
squawk range, using this syntax: `'#('<counter id> ['=' <start> ['+'<increment>]])`.

Or, in plain English:

*A hash and bracket - '#(' - followed by the name of the counter. 
Then, optionally, a start value (gets set to zero if omitted) and,
also optionally, an increment value (gets set to one if omitted). 
End the counter with a closing bracket: ')'.*

Here's how you would have written the above range to achieve incrementing
the 'x' in "Aerodrome #x":

```json5
  {
    "mode3": "7040-7047 #(x)", 
      "comment": "Aerodrome #=(x)"
  }
```

The `#(x)` is interpreted by the compiler as: for this range; 
add counter '`x`'. As no start or increment value 
was specified; set to zero and one respectively.

So, if you wanted to start with ten and also increment
by ten you would instead have written `#(x = 10 + 10)`, which would 
have named the aerodromes "Aerodrome #10", "Aerodrome #20", "Aerodrome #30"
and so on. As you might imagine, `#(c+10)` would have produced
the series "0", "10", "20" ... and so on.

You might want multiple counters for a range, like in this example:

```json
  {
    "mode3": "7040-7047 #(unit), #(tail = 400 + 10)",
    "comment": "CAP - Roman 1-=(unit) (tail no. =(tail))"
  }
```

This would have been compiled into this output:

```json5
[
  {
    "mode3": "7040",
    "comment": "CAP - Roman 1-1 (tail no. 400)"
  },
  {
    "mode3": "7041",
    "comment": "CAP - Roman 1-2 (tail no. 410)"
  },
  {
    "mode3": "7041",
    "comment": "CAP - Roman 1-3 (tail no. 420)"
  }
  // and so on...
]
```

## Comments

The squawk compiler currently supports JSON 5 comments
like in the above examples. There are two types of comments: 
Single line and multiline.

All comments are always removed from the output result.

### Single line comments

Single line comment starts with "`//`" and goes on till the end of that
line:

```json5

[
  {
    "mode3": "7040", // this comment will be removed and ends here.
    "comment": "CAP - Roman 1-1 (tail no. 400)"
  }
]
```

### Multiline comments

Multiline comments start with "`/*`" and ends with "`*/`",
on the same line or (usually) on a different line.

```json5

[
  /* 
     +------------------------------------+
     |  Multiline comments can be useful  |
     |  as they allow for more text and   |
     |  attracts attention                | 
     +------------------------------------+ 
  */
  {
    "mode3": "7040", 
    "comment": "CAP - Roman 1-1 (tail no. 400)"
  }
]
```

## Advanced templating

Consider this squawk range again:

```json5
[  
  { 
    "mode3": "7040",
    "comment": "Aerodrome #0"
  },
  {
    "mode3": "7041",
    "comment": "Aerodrome #1"
  },
  {
    "mode3": "7042",
    "comment": "Aerodrome #2"
  },
  // and so on...
]
```

That might be great if all controllers know which aerodrome is #0, #1, #2
etc.but, maybe, it would be even better if the actual aerodrome names could 
be included in the squawk comment. That, of course, might change
from one map to another, or even between events on the same map.

To allow for more details the squawk compiler can take a second "data" 
file as its input to resolve dynamic values. Here's how you could 
achieve this by modifying the original squawk range declaration example:

## Template file with variables

```json5
// data.json5
{
  "__comments":"THIS FILE IS AN EXAMPLE AND WILL NOT BE LOADED BECAUSE enable=false",
  "enable": true,
  "transponders" : [
    {
      "mode3": "7001",
      "comment": "VFR"
    },
    {
      "mode3": "7040-7047 #(n)",
      "comment": "$(Aerodrome #=(n))"
    }
  ]
}

```

As we have already seen this would name the aerodromes "Aerodrome #0",
"Aerodrome #1", and so on. But by surrounding the result inside a
variable qualifier "$(" and ")" you are instructing the compiler
that this is a variable and that it should try and look it up
in a data file you're supplying. The data file is also a JSON file
with a single list of key/value pairs, like this:

### Example data file

```json5
// data-syria.json
{
  "Aerodrome #0": "Incirlik AB",
  "Aerodrome #1": "Ramat David AB",
  "Aerodrome #2": "Paphos",
  "AWACS #0": "Magic 1-1",
  "AWACS #1": "Overlord 1-1",
  "AWACS #2": "Wizard 1-1",
}
```
By supplying this file to the compiler it will automatically substitute 
all variables with the corresponding value it finds in the data file. 
For the above examples this is the resulting output:

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
    "comment": "Aerodrome #3"
  },
  {
    "mode3": "7044",
    "comment": "Aerodrome #4"
  },
    // and so on...
]
```

The fact that the values are kept in separate files means you are free 
to create very few squawk layout templates, perhaps just one,
and then recompile it with different data files to suit your needs.

Also, please note how some of the above aerodromes didn't get resolved 
to specific aerodrome names. The reason for that is that they 
("Aerodrome #3" and above) aren't available in the example data file.
When the compiler can't resolve a variable it simply retains its name,
removing the qualifier tokens: `$(` and `)` .

## Running the compiler

You invoke the LotATC squawk classification compiler from the command line. The exact approach 
differs between Windows and Mac but the arguments passed to it is the same for both:

The first argument needs to be the name of (or path to) your template file 
(the one that contains your ranges, counters and variable references). This 
argument is necessary and the compiler will end with an error message if it is omitted.

All other arguments are optional. Some of them are just "flags" while others
are values preceded by the argument identifier:

### --write (-w)

This value specifies the name of (or path to) the LotATC you want to create
and upload to your LotATC server. 

Example:
```
lascc myLotATC_template.json5 -w myLotATC.json
lascc myLotATC_template.json5 --write myLotATC.json
```

### --overwrite (-o)

This is just a flag (no value is expected) that instructs the 
compiler to always overwrite the output file (see [--write](#--write--w) 
above) if it already exists. If you omit this flag and the compiler 
detects that the output file already exists it will end with an error. 

Example:
```
lascc myLotATC_template.json5 -w myLotATC.json -o
```

### --verbose (-v)

This flag specifies to the compiler that you want it to output its
progress. This can be useful for understanding what happens when
you run the compiler manually (as opposed to being a part of an 
automated process, such as a DevOps build flow for example).

> NOTE: The compiler currently provides little output but
> will improve in future updates.

### --data (-d)

This value specifies the name of (or path to) a data file, to be used
by the compiler to resolve variables. As an example, if you place the 
[example data file (above)](#example-data-file)
in your compiler folder, along with your 
[example template file](#template-file-with-variables),
this is how you could invoke the compiler:

Example:
```
lascc myLotATC_template.json5 -w myLotATC.json -o -d data-syria.json
```

### --help (-h or -?)

Specifies to the compiler that you want it to just show a help. 
This will produce a list of arguments available and a short explanation.

## Windows

-- TODO --

## Mac

-- TODO --

[lot-atc-json-document]: https://www.lotatc.com/documentation/client/classification.html#transponder-format