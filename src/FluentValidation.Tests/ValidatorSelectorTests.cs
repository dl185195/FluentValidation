#region License
// Copyright (c) .NET Foundation and contributors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// The latest version of this file can be found at https://github.com/FluentValidation/FluentValidation
#endregion

namespace FluentValidation.Tests;

using Xunit;
using System.Threading.Tasks;

public class ValidatorSelectorTests {

	[Fact]
	public void MemberNameValidatorSelector_returns_true_when_property_name_matches() {
		var validator = new InlineValidator<TestObject> {
			v => v.RuleFor(x => x.SomeProperty).NotNull()
		};

		var result = validator.Validate(new TestObject(), v => v.IncludeProperties("SomeProperty"));
		result.Errors.Count.ShouldEqual(1);
	}

	[Fact]
	public void Does_not_validate_other_property() {
		var validator = new InlineValidator<TestObject> {
			v => v.RuleFor(x => x.SomeOtherProperty).NotNull()
		};

		var result = validator.Validate(new TestObject(),v => v.IncludeProperties("SomeProperty"));
		result.Errors.Count.ShouldEqual(0);
	}

	[Fact]
	public void Validates_property_using_expression() {
		var validator = new InlineValidator<TestObject> {
			v => v.RuleFor(x => x.SomeProperty).NotNull()
		};

		var result = validator.Validate(new TestObject(), v => v.IncludeProperties(x => x.SomeProperty));
		result.Errors.Count.ShouldEqual(1);
	}

	[Fact]
	public void Does_not_validate_other_property_using_expression() {
		var validator = new InlineValidator<TestObject> {
			v => v.RuleFor(x => x.SomeOtherProperty).NotNull()
		};

		var result = validator.Validate(new TestObject(), v => v.IncludeProperties(x => x.SomeProperty));
		result.Errors.Count.ShouldEqual(0);
	}

	[Fact]
	public void Validates_nullable_property_with_overriden_name_when_selected() {

		var validator = new InlineValidator<TestObject> {
			v => v.RuleFor(x => x.SomeNullableProperty.Value)
				.GreaterThan(0)
				.When(x => x.SomeNullableProperty.HasValue)
				.OverridePropertyName("SomeNullableProperty")
		};

		var result = validator.Validate(new TestObject { SomeNullableProperty = 0 },
			v => v.IncludeProperties(x => x.SomeNullableProperty));
		result.Errors.Count.ShouldEqual(1);
	}

	[Fact]
	public void Includes_nested_property() {
		var validator = new TestValidator {
			v => v.RuleFor(x => x.Surname).NotNull(),
			v => v.RuleFor(x => x.Address.Id).NotEqual(0)
		};

		var result = validator.Validate(new Person { Address = new Address() }, v => v.IncludeProperties("Address.Id"));
		result.Errors.Count.ShouldEqual(1);
		result.Errors[0].PropertyName.ShouldEqual("Address.Id");
	}

	[Fact]
	public void Includes_nested_property_using_expression() {
		var validator = new TestValidator {
			v => v.RuleFor(x => x.Surname).NotNull(),
			v => v.RuleFor(x => x.Address.Id).NotEqual(0)
		};

		var result = validator.Validate(new Person { Address = new Address() }, v => v.IncludeProperties(x => x.Address.Id));
		result.Errors.Count.ShouldEqual(1);
		result.Errors[0].PropertyName.ShouldEqual("Address.Id");

	}

	[Fact]
	public void Can_use_property_with_include() {
		var validator = new TestValidator();
		var validator2 = new TestValidator();
		validator2.RuleFor(x => x.Forename).NotNull();
		validator.Include(validator2);

		var result = validator.Validate(new Person(), v => v.IncludeProperties("Forename"));
		result.IsValid.ShouldBeFalse();
	}

	[Fact]
	public void Executes_correct_rule_when_using_property_with_include() {
		var validator = new TestValidator();
		var validator2 = new TestValidator();
		validator2.RuleFor(x => x.Forename).NotNull();
		validator2.RuleFor(x => x.Surname).NotNull();
		validator.Include(validator2);

		var result = validator.Validate(new Person(), v => v.IncludeProperties("Forename"));
		result.Errors.Count.ShouldEqual(1);
		result.Errors[0].PropertyName.ShouldEqual("Forename");
	}

	[Fact]
	public async Task Executes_correct_rule_when_using_property_with_include_async() {
		var validator = new TestValidator();
		var validator2 = new TestValidator();
		validator2.RuleFor(x => x.Forename).NotNull();
		validator2.RuleFor(x => x.Surname).NotNull();
		validator.Include(validator2);

		var result = await validator.ValidateAsync(new Person(), v => v.IncludeProperties("Forename"));
		result.Errors.Count.ShouldEqual(1);
		result.Errors[0].PropertyName.ShouldEqual("Forename");
	}

	[Fact]
	public void Executes_correct_rule_when_using_property_with_nested_includes() {
		var validator3 = new TestValidator();
		validator3.RuleFor(x => x.Age).GreaterThan(0);

		// In the middle validator ensure that the Include statement is
		// before the additional rules in order to trigger the case reported in
		// https://github.com/FluentValidation/FluentValidation/issues/1989
		var validator2 = new TestValidator();
		validator2.Include(validator3);
		validator2.RuleFor(x => x.Orders).NotEmpty();

		var validator = new TestValidator();
		validator.Include(validator2);

		var result = validator.Validate(new Person(), v => v.IncludeProperties("Age"));
		result.Errors.Count.ShouldEqual(1);
		result.Errors[0].PropertyName.ShouldEqual("Age");

		result = validator.Validate(new Person { Age = 1 }, v => v.IncludeProperties("Age"));
		result.Errors.Count.ShouldEqual(0);
	}

	private class TestObject {
		public object SomeProperty { get; set; }
		public object SomeOtherProperty { get; set; }
		public decimal? SomeNullableProperty { get; set; }
	}
}
