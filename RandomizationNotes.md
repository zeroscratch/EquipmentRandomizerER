# Randomization Rates
To make a long story short, we don't have enough statistical data to definitely say that the randomization rates are skewed. We have to generate the data which requires a substantial expenditure of effort to write the testing code in a way that perfectly matches the current randomizer implementation (otherwise, the test isn't accurate to our real-world results).

## Thought Experiment: Halberds
1. Spirit Glaive
2. Halberd
3. Banished Knight's Halberd
4. Lucerne
5. Nightrider Glaive
6. Gargoyle's Halberd
7. Gargoyle's Black Halberd
8. Golden Halberd
9. Dragon Halberd
10. Loretta's War Sickle
11. Commander's Standard

## Hypothesis
In theory, any halberd drop should have a 1/11 chance to be a vanilla drop.

## Test
Run the shuffle algorithm N times, and then collect percentages for each weapon, and compare to expectation.

## Expectation
The formula for expectation is simple:
```python
expectation = 1. / number_of_weapons_in_group
```
because we use a uniform shuffle method.

## Real Rates
The formula for real rates is:
```python
real_rates = number_of_times_weapons_appears_vanilla / number_of_times_shuffled
```
If the shuffle algorithm is uniform, then we expect:
```python
real_rates == expectation
```
to be near true. We can accept some margin of error by defining another formula:
```python
def acceptable_rate(margin_of_error):
    return abs(real_rates - expectation) < margin_of_error
```

## Test Implementation Strategy
We can do this in one of two ways:

#### Python Aided
1. Use modified C# randomizer code to run the randomization for all of the weapon pools
2. Save the outputs to a bunch of files using the `logItem`-related functions already in the randomizer
3. Use Python code to analyze the real vs expected rates by aggregating the results from the files
We still have to determine the number of weapons per pool, which would be a pain to do manually.

#### C Sharp centric
1. Write C# code that runs all of the seeds in one go, then writes the results to a single file
2. We can still use Python to generate graphs

## Test Implementation
Run a bunch of seeds and collect data.

Each seed is generated using the function:
```csharp
private string createSeed() { return Guid.NewGuid().ToString(); }
```
Then the random number generator is instantiated with the integer value:
```csharp
int sequence = hashStringToInteger(_seed);
_random = new Random(sequence);
```
and the hash function is:
```csharp
private static int hashStringToInteger(string input)
{
    byte[] array = Encoding.UTF8.GetBytes(input);
    byte[] hashData = SHA256.HashData(array);
    IEnumerable<byte[]> chunks = hashData.Chunk(4);
    // if we have a toggle for smithing cost, could choose different range of chunks,
    // however what would the step be if there are more toggles?
    return chunks.Aggregate(0, (current, chunk) => current ^ BitConverter.ToInt32(chunk));
}
```
For the test to be accurate to the randomizer, we need to perfectly replicate the number of times the random seed generator is called, and it needs to be done in the right order, because the output is deterministic once the seed has been chosen.
