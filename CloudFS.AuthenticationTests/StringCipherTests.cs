﻿/*
The MIT License(MIT)

Copyright(c) 2016 IgorSoft

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IgorSoft.CloudFS.Authentication;

namespace IgorSoft.CloudFS.AuthenticationTests
{
    [TestClass]
    public class StringCipherTests
    {
        private const string plainText = "PlainText";
        private const string passPhrase = "PassPhrase";

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void EncryptUsing_WherePassPhraseIsEmpty_ReturnsPlainText()
        {
            var cipherText = plainText.EncryptUsing(string.Empty);

            Assert.AreEqual(plainText, cipherText);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void EncryptUsing_WherePassPhraseIsSet_Succeeds()
        {
            var cipherText = plainText.EncryptUsing(passPhrase);

            Assert.IsNotNull(cipherText, "Encrypted text is null");
            StringAssert.DoesNotMatch(cipherText, new Regex($".*{plainText}.*"), "Encrypted text contains plain text");
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void DecryptUsing_WherePassPhraseIsEmpty_ReturnsPlainText()
        {
            var cipherText = plainText;

            var decryptedText = cipherText.DecryptUsing(string.Empty);

            Assert.AreEqual(plainText, decryptedText);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void DecryptUsing_WherePassPhraseIsMatches_Succeeds()
        {
            var cipherText = plainText.EncryptUsing(passPhrase);

            var decryptedText = cipherText.DecryptUsing(passPhrase);

            Assert.AreEqual(plainText, decryptedText, "Decrypted text is different from plain text");
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void DecryptUsing_WherePassPhraseDiffers_Fails()
        {
            var cipherText = plainText.EncryptUsing(passPhrase);

            var decryptedText = cipherText.DecryptUsing(new string(passPhrase.Reverse().ToArray()));

            Assert.AreEqual(cipherText, decryptedText, "Decrypted text is different from plain text");
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Encrypt_WherePassPhraseIsEmpty_Throws()
        {
            StringCipher.Encrypt(plainText, string.Empty);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Decrypt_WherePassPhraseIsEmpty_Throws()
        {
            StringCipher.Decrypt(plainText, string.Empty);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void Encrypt_WherePassPhraseIsSet_Succeeds()
        {
            var cipherText = StringCipher.Encrypt(plainText, passPhrase);

            Assert.IsNotNull(cipherText, "Encrypted text is null");
            StringAssert.DoesNotMatch(cipherText, new Regex($".*{plainText}.*"), "Encrypted text contains plain text");
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void Encrypt_WherePassPhraseIsSetAndPlainTextIsEmpty_Succeeds()
        {
            var cipherText = StringCipher.Encrypt(string.Empty, passPhrase);

            Assert.IsNotNull(cipherText, "Encrypted text is null");
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void Decrypt_WherePassPhraseMatches_Succeeds()
        {
            var cipherText = StringCipher.Encrypt(plainText, passPhrase);

            var decryptedText = StringCipher.Decrypt(cipherText, passPhrase);

            Assert.AreEqual(plainText, decryptedText, "Decrypted text is different from plain text");
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(CryptographicException))]
        public void Decrypt_WherePassPhraseDiffers_Fails()
        {
            var cipherText = StringCipher.Encrypt(plainText, passPhrase);

#pragma warning disable S1481 // Unused local variables should be removed
            var decryptedText = StringCipher.Decrypt(cipherText, new string(passPhrase.Reverse().ToArray()));
#pragma warning restore S1481 // Unused local variables should be removed
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void Encrypt_WherePassPhraseIsSet_IsSaltedCorrectly()
        {
            var cipherText1 = StringCipher.Encrypt(plainText, passPhrase);
            var cipherText2 = StringCipher.Encrypt(plainText, passPhrase);

            Assert.IsNotNull(cipherText1, "Encrypted text is null");
            Assert.IsNotNull(cipherText2, "Encrypted text is null");
            Assert.AreNotEqual(cipherText2, cipherText1, "Encrypted text is not salted");
        }
    }
}
