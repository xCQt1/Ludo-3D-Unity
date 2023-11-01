using System.Collections;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;

public class Piece : MouseClickable
{
    protected Field currentField;
    public Player player;
    private NumberGenerator gen;
    public int FieldsMoved {get; private set; } = 0;
    public float MoveAnimationDuration = .5f; // in millisecs
    public bool inAnimation {get; private set;}
    
    public bool CanMove(int fields) => GameHandler.Instance.currentPlayer == player &&      // der Spieler der Figur am Zug ist
                                       NumberGenerator.Instance.lastNumber != 0 &&          // die letzte gewürfelte Nummer keine 0 ist
                                       ((currentField is not BoxField && 
                                            GetField(fields) is not null &&
                                            (GetField(fields).IsFree || GetField(fields)?.GetCurrentPiece().player != this.player)) || 
                                       (currentField is BoxField && fields == 6 && 
                                            (player.spawnField.IsFree || player.spawnField.GetCurrentPiece().player != player)));
    public bool CanCapture(int fields) => CanMove(fields) && !GetField(fields).IsFree;
    // bedeutet: Kann sich um fields Felder bewegen und auf dem Zielfeld steht eine Figur
    public bool CanClearStartField(int fields) => currentField is SpawnField && CanMove(fields);
    public bool CanLeaveBox(int fields) => currentField is BoxField && CanMove(fields);
    
    // Start is called before the first frame update
    void Start()
    {
        SetColor();
        MovePieceToCurrentField();
        gen = NumberGenerator.Instance;
    }

    private void SetColor() {
        GetComponentInChildren<Renderer>().material.color = player.color;
    }

    public void SetStartField(BoxField field) {
        currentField = field;
        currentField.PlacePiece(this);
        MovePieceToCurrentField();
    }

    protected void MovePieceToCurrentField() {
        transform.position = currentField.transform.position;
    }

    public void Capture() {
        foreach (BoxField boxField in player.boxFields) {
            if (boxField.IsFree) {
                MoveToField(boxField);
                FieldsMoved = 0;
                return;
            }
        }
        Debug.LogError("Critical Error: No boxfield empty!");
    }

    public void Move() {
        if (inAnimation || !CanMove(gen.lastNumber)) return;
        if (currentField is BoxField) {
            MoveToField(player.spawnField);
        } else {
            MoveFields(gen.lastNumber);
        }
    }

    public bool MoveToField(Field field) {
        if (field is null) return false;
        if (field.PlacePiece(this)) {
            Debug.Log($"{player.name} has moved a piece");
            currentField.RemoveCurrentPiece();
            currentField = field;
            currentField.PlacePiece(this);

            NumberGenerator.Instance.StartAnimation(this);
            StartCoroutine(AnimatePieceMove(field));
            return true;
        } else {
            Debug.Log("Piece move declined: Target field isnt empty");
            return false;
        }
        
    }

    public Field GetField(int numberOfFields) {
        Field targetField = currentField;
        for (int i=0; i<numberOfFields; i++) {
            if (targetField is null) return null;
            targetField = targetField.endField is null || targetField.endField.player != this.player ? targetField.nextField : targetField.endField;
        }
        return targetField;
    }

    public void MoveFields(int numberOfFields) {
        if (MoveToField(GetField(numberOfFields))) FieldsMoved += numberOfFields;
    }

    private IEnumerator AnimatePieceMove(Field newField) {
        inAnimation = true;

        while(NumberGenerator.Instance.inAnimation) {
            yield return new WaitForSeconds(1);
        }

        float timeElapsed = 0f;
        Vector3 startPos = transform.position;
        Vector3 targetPos = newField.transform.position;

        while (timeElapsed < MoveAnimationDuration) {
            transform.position = Vector3.Lerp(startPos, targetPos, timeElapsed/MoveAnimationDuration);
            Physics.SyncTransforms();
            timeElapsed += Time.deltaTime;
            yield return null;
        }
        
        inAnimation = false;
        player.hasMoved = true;
        MovePieceToCurrentField();
    }

    public override void OnClick() {
        Move();
    }

    protected override Color DetermineColor() {
        return player != GameHandler.Instance.currentPlayer || gen.lastNumber == 0 ? Color.grey : CanMove(gen.lastNumber) ? Color.green : Color.red;
    }
}