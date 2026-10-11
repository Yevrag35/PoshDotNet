# Proves the runtime behavior behind PDN1005: two parameters with the same Position in one parameter set.
# Runs in Windows PowerShell 5.1 and PowerShell 7, so keep it 5.1-compatible and ASCII. Run it through
# ..\Invoke-ReproCorpus.ps1, which gives each edition a new process.

BeforeAll {
	$types = Add-Type -Path (Join-Path $PSScriptRoot 'DuplicatePositions.cs') -PassThru
	Import-Module -Assembly $types[0].Assembly
}

Describe 'PDN1005 reports these: a positional argument fails to bind' {
	It '<Command> one' -ForEach @(
		@{ Command = 'Test-Pdn1005NoSetPair' }
		@{ Command = 'Test-Pdn1005NamedSetPair' }
		@{ Command = 'Test-Pdn1005OtherSetPair' }
		@{ Command = 'Test-Pdn1005AllSetsSpelling' }
		@{ Command = 'Test-Pdn1005EmptySetName' }
		@{ Command = 'Test-Pdn1005InheritedPair' }
		@{ Command = 'Test-Pdn1005FieldPair' }
		@{ Command = 'Test-Pdn1005NegativePair' }
		@{ Command = 'Test-Pdn1005OtherTypeKeepsBase' }
		@{ Command = 'Test-Pdn1005FieldKeepsBase' }
	) {
		{ & $Command one } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException]) `
			-ErrorId 'AmbiguousPositionalParameterNoName,*' -ExpectedMessage 'Cannot bind positional parameters because no names were given.*'
	}

	It 'Get-Command -Syntax shows one of the two parameters' {
		$syntax = Get-Command Test-Pdn1005NoSetPair -Syntax | Out-String
		$syntax | Should -Match '\[-Second\]'
		$syntax | Should -Not -Match '\[-First\]'
	}

	It 'binds when the call names both parameters' {
		Test-Pdn1005NoSetPair -First a -Second b | Should -Be 'set=__AllParameterSets First=a Second=b'
	}

	It 'binds a positional argument once the call names one of the two' {
		Test-Pdn1005NoSetPair -First a b | Should -Be 'set=__AllParameterSets First=a Second=b'
		Test-Pdn1005OtherSetPair -A1 a b | Should -Be 'set=A A1=a A2=b B1='
	}
}

Describe 'PDN1005 leaves these alone: a positional argument binds' {
	It 'binds one argument to both a parameter in every set and one in a named set' {
		Test-Pdn1005NoSetAndNamedSet one | Should -Be 'set=A First=one Second=one'
	}

	It 'gives a ValueFromRemainingArguments parameter the arguments the other leaves' {
		Test-Pdn1005RemainingArguments one two three | Should -Be 'set=__AllParameterSets First=one Rest=two,three'
	}

	It 'treats set names that differ in case as two sets' {
		Test-Pdn1005SetNameCase one | Should -Be 'set=ByName First=one Second='
	}

	It 'lets one parameter have the same position in two sets' {
		Test-Pdn1005OneParameterTwoSets one | Should -Be 'set=A Name=one'
		Test-Pdn1005OneParameterTwoSets one -Force | Should -Be 'set=B Name=one'
	}

	It 'reads only public instance members as parameters' {
		Test-Pdn1005NotParameters one | Should -Be 'set=__AllParameterSets Visible=one'
		(Get-Command Test-Pdn1005NotParameters).Parameters.Keys | Should -Not -Contain 'Internal'
		(Get-Command Test-Pdn1005NotParameters).Parameters.Keys | Should -Not -Contain 'Shared'
	}

	It 'lets a derived property of the same type hide the base parameter' {
		Test-Pdn1005SameTypeHidesBase one | Should -Be 'set=__AllParameterSets Second=one'
		(Get-Command Test-Pdn1005SameTypeHidesBase).Parameters.Keys | Should -Not -Contain 'First'
	}

	It 'lets an override without [Parameter] hide the base parameter' {
		Test-Pdn1005OverrideHidesBase one | Should -Be 'set=__AllParameterSets Second=one'
		(Get-Command Test-Pdn1005OverrideHidesBase).Parameters.Keys | Should -Not -Contain 'First'
	}

	It 'treats Position = int.MinValue as no position' {
		{ Test-Pdn1005MinValuePair one } | Should -Throw -ErrorId 'PositionalParameterNotFound,*'
		Test-Pdn1005MinValuePair -First a -Second b | Should -Be 'set=__AllParameterSets First=a Second=b'
	}
}
